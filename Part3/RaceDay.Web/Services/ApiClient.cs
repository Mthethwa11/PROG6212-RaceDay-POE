using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Services;

public class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _http;

    public ApiClient(IHttpClientFactory factory, IHttpContextAccessor http)
    {
        _factory = factory;
        _http = http;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path) =>
        SendAsync<T>(HttpMethod.Get, path, null);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, path, CreateJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null) =>
        SendAsync<T>(HttpMethod.Put, path, CreateJson(body));

    public Task<ApiResult<object>> DeleteAsync(string path) =>
        SendAsync<object>(HttpMethod.Delete, path, null);

    public Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file, string fieldName = "file")
    {
        var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());

        if (!MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
        {
            mediaType = new MediaTypeHeaderValue("application/octet-stream");
        }
        fileContent.Headers.ContentType = mediaType;

        form.Add(fileContent, fieldName, file.FileName);
        return SendAsync<T>(HttpMethod.Post, path, form);
    }

    // ---------------------------------------------------------------- core

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, HttpContent? content)
    {
        try
        {
            var client = _factory.CreateClient("RaceDayApi");
            using var request = new HttpRequestMessage(method, path.TrimStart('/')) { Content = content };
            AttachCookies(request);

            using var response = await client.SendAsync(request);
            StoreCookies(response);

            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                T? data = default;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    data = JsonSerializer.Deserialize<T>(text, JsonOptions);
                }
                return new ApiResult<T>(true, response.StatusCode, data, null);
            }

            var error = await ReadErrorAsync(response);
            return new ApiResult<T>(false, response.StatusCode, default, error);
        }
        catch (HttpRequestException)
        {
            return Failure<T>(HttpStatusCode.ServiceUnavailable,
                "We couldn't reach the RaceDay service. Please try again in a moment.");
        }
        catch (TaskCanceledException)
        {
            return Failure<T>(HttpStatusCode.GatewayTimeout,
                "The RaceDay service took too long to respond. Please try again.");
        }
        catch (JsonException)
        {
            return Failure<T>(HttpStatusCode.BadGateway,
                "We received an unexpected response from the RaceDay service.");
        }
    }

    private static ApiResult<T> Failure<T>(HttpStatusCode status, string message) =>
        new(false, status, default, message);

    private static HttpContent? CreateJson(object? body) =>
        body is null ? null : JsonContent.Create(body, body.GetType(), null, JsonOptions);

    // ---------------------------------------------------------------- cookies
    // The API identifies the user with cookies. The browser never talks to the API,
    // so this MVC app keeps those cookies in its own session and sends them on each call.

    private ISession Session =>
        _http.HttpContext?.Session ?? throw new InvalidOperationException("No active HTTP session.");

    private Dictionary<string, string> LoadCookies()
    {
        var json = Session.GetString(SessionKeys.ApiCookies);
        if (string.IsNullOrEmpty(json)) return new Dictionary<string, string>();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    private void AttachCookies(HttpRequestMessage request)
    {
        var jar = LoadCookies();
        if (jar.Count == 0) return;
        var header = string.Join("; ", jar.Select(c => $"{c.Key}={c.Value}"));
        request.Headers.TryAddWithoutValidation("Cookie", header);
    }

    private void StoreCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies)) return;

        var jar = LoadCookies();
        foreach (var raw in setCookies)
        {
            var parts = raw.Split(';');
            var pair = parts[0].Split('=', 2);
            if (pair.Length != 2) continue;

            var name = pair[0].Trim();
            var value = pair[1].Trim();

            if (string.IsNullOrEmpty(value) || IsExpired(parts))
                jar.Remove(name);
            else
                jar[name] = value;
        }

        Session.SetString(SessionKeys.ApiCookies, JsonSerializer.Serialize(jar));
    }

    private static bool IsExpired(string[] cookieParts)
    {
        foreach (var part in cookieParts.Skip(1))
        {
            var p = part.Trim();
            if (p.StartsWith("max-age=", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(p[8..], out var seconds) && seconds <= 0)
                return true;

            if (p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase) &&
                DateTimeOffset.TryParse(p[8..], out var expires) && expires < DateTimeOffset.UtcNow)
                return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- friendly errors

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        string? message = null;
        string? text = null;
        try
        {
            text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                message = ExtractMessage(doc.RootElement);
            }
        }
        catch (JsonException)
        {
            // Not JSON. Use short plain text, but never show HTML error pages to the user.
            if (!string.IsNullOrWhiteSpace(text) && text.Length <= 200 && !text.TrimStart().StartsWith('<'))
                message = text.Trim();
        }

        return !string.IsNullOrWhiteSpace(message) ? message : DefaultMessage(response.StatusCode);
    }

    private static string? ExtractMessage(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String) return root.GetString();
        if (root.ValueKind != JsonValueKind.Object) return null;

        if (TryGet(root, "errors", out var errors))
        {
            var messages = new List<string>();
            CollectStrings(errors, messages);
            if (messages.Count > 0) return string.Join(" ", messages.Distinct());
        }

        foreach (var name in new[] { "message", "error", "detail", "title" })
        {
            if (TryGet(root, name, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
                return value.GetString();
        }
        return null;
    }

    private static bool TryGet(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static void CollectStrings(JsonElement element, List<string> output)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var s = element.GetString();
                if (!string.IsNullOrWhiteSpace(s)) output.Add(s);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) CollectStrings(item, output);
                break;
            case JsonValueKind.Object:
                // ASP.NET Identity errors look like { "code": "...", "description": "..." }
                if (TryGet(element, "description", out var description))
                {
                    CollectStrings(description, output);
                }
                else
                {
                    foreach (var property in element.EnumerateObject()) CollectStrings(property.Value, output);
                }
                break;
        }
    }

    private static string DefaultMessage(HttpStatusCode status) => (int)status switch
    {
        400 => "Please check the details you entered and try again.",
        401 => "Please log in to continue.",
        403 => "You don't have permission to do that.",
        404 => "We couldn't find what you were looking for.",
        409 => "That clashes with information that already exists.",
        >= 500 => "Something went wrong on our side. Please try again.",
        _ => "Something went wrong. Please try again."
    };
}
