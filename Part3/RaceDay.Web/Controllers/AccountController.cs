using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IApiClient _api;

    public AccountController(IApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------ Register

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToHome();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToHome();
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<object>("api/auth/register", new
        {
            fullName = model.FullName.Trim(),
            email = model.Email.Trim(),
            password = model.Password,
            role = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty,
                result.Error ?? "We couldn't create your account. Please try again.");
            return View(model);
        }

        // Make sure no API cookies from registering are kept; the user logs in properly next.
        HttpContext.Session.Clear();

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    // ------------------------------------------------------------ Login

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToHome();
        if (!ModelState.IsValid) return View(model);

        // Start from a clean session so cookies from an earlier user are never sent.
        HttpContext.Session.Clear();

        var result = await _api.PostAsync<UserProfileDto>("api/auth/login", new
        {
            email = model.Email.Trim(),
            password = model.Password
        });

        if (!result.Success || result.Data is null || string.IsNullOrEmpty(result.Data.UserId))
        {
            ModelState.AddModelError(string.Empty,
                result.IsUnauthorized
                    ? "Incorrect email or password. Please try again."
                    : result.Error ?? "We couldn't log you in. Please try again.");
            return View(model);
        }

        var user = result.Data;
        HttpContext.Session.SignIn(user.UserId, user.Role, user.FullName);

        // Load the profile picture (if there is one) so it can be shown in the navigation bar.
        var me = await _api.GetAsync<UserProfileDto>("api/users/me");
        HttpContext.Session.SetProfilePictureUrl(me.Success ? me.Data?.ProfilePictureUrl : null);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }
        return RedirectToHome();
    }

    // ------------------------------------------------------------ Logout

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Session.IsAuthenticated())
        {
            // Tell the API to end its session too; we ignore the result because we log out regardless.
            await _api.PostAsync<object>("api/auth/logout");
        }

        HttpContext.Session.SignOut();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    // ------------------------------------------------------------ Access denied

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome() =>
        HttpContext.Session.IsOrganiser()
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
}
