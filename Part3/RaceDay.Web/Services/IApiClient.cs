using System.Net;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Services;

/// <summary>The outcome of a call to the Part 2 API. Controllers check Success and show Error if it failed.</summary>
public record ApiResult<T>(bool Success, HttpStatusCode StatusCode, T? Data, string? Error)
{
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
}

/// <summary>
/// The only way the MVC app talks to the outside world. All data and all file uploads go
/// through the Part 2 API. There is no database access and no direct Azure access here.
/// </summary>
public interface IApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path);
    Task<ApiResult<T>> PostAsync<T>(string path, object? body = null);
    Task<ApiResult<T>> PutAsync<T>(string path, object? body = null);
    Task<ApiResult<object>> DeleteAsync(string path);
    Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file, string fieldName = "file");
}
