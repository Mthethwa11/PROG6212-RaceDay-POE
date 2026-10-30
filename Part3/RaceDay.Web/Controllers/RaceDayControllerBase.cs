using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
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

    /// <summary>
    /// Loads an event from the API and checks it belongs to the logged-in Organiser.
    /// Returns the event, or a redirect to use instead when something is wrong.
    /// </summary>
    protected async Task<(EventDto? Event, IActionResult? Failure)> LoadOwnedEventAsync(IApiClient api, int eventId)
    {
        var result = await api.GetAsync<EventDto>($"api/events/{eventId}");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return (null, redirect);

        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound
                ? "That event could not be found."
                : result.Error;
            return (null, RedirectToAction("Index", "OrganiserEvents"));
        }

        if (!IsCurrentUser(result.Data.OrganiserId))
        {
            return (null, RedirectToAction("AccessDenied", "Account"));
        }

        return (result.Data, null);
    }
}
