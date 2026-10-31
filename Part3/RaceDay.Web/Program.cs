using System.Globalization;
using Microsoft.AspNetCore.Localization;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Session keeps the logged-in user's ID, role and the API cookies between pages.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// The API address comes from configuration (appsettings.json) and can be overridden
// with the environment variable Api__BaseUrl, which is how Docker sets it.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

// Off by default. Only switched on (Api__AllowUntrustedCertificate=true) when the container
// talks to the API's local development certificate, which the container does not trust.
var allowUntrustedCertificate = builder.Configuration.GetValue<bool>("Api:AllowUntrustedCertificate");

builder.Services.AddHttpClient("RaceDayApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // We pass the API cookies along ourselves (stored in the MVC session), so the handler must not manage them.
    var handler = new HttpClientHandler { UseCookies = false };

    if (allowUntrustedCertificate)
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
});

builder.Services.AddScoped<IApiClient, ApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Always use one culture (en-US) so decimals like 5.5 and dates bind and display the same
// on every PC, whatever its regional settings (some use a comma as the decimal separator).
var supportedCultures = new[] { new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
