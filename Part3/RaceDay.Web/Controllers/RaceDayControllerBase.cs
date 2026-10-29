using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared behaviour for controllers that call the API.</summary>
public abstract class RaceDayControllerBase : Controller
{
    /// <summary>
    /// If the API said the user is no longer logged in (401) or isn't allowed (403),
    /// returns the redirect to use. Returns null when there is nothing to handle.
    /// </summary>
    protected IActionResult? HandleAuthFailure<T>(ApiResult<T> result)
    {
        if (result.IsUnauthorized)
        {
            HttpContext.Session.SignOut();
            TempData["Error"] = "Your session has expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        if (result.IsForbidden)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return null;
    }

    protected bool IsCurrentUser(string? userId) =>
        !string.IsNullOrEmpty(userId) &&
        string.Equals(userId, HttpContext.Session.GetUserId(), StringComparison.Ordinal);
}
