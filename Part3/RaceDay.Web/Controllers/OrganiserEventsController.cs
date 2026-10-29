using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser event management: list, create, edit and delete. All data goes through the API.</summary>
[RoleAuthorize(Roles.Organiser)]
[Route("organiser/events")]
public class OrganiserEventsController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public OrganiserEventsController(IApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------ List

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (!result.Success)
        {
            ViewData["LoadError"] = result.Error;
        }
        return View(result.Data ?? new List<EventDto>());
    }

    // ------------------------------------------------------------ Create

    [HttpGet("create")]
    public IActionResult Create() => View(new EventFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var created = await _api.PostAsync<EventDto>("api/events", BuildRequest(model));

        var redirect = HandleAuthFailure(created);
        if (redirect != null) return redirect;

        if (!created.Success || created.Data is null)
        {
            ModelState.AddModelError(string.Empty,
                created.Error ?? "We couldn't create the event. Please try again.");
            return View(model);
        }

        var eventId = created.Data.EventId;

        if (model.BannerFile is { Length: > 0 })
        {
            var upload = await _api.UploadAsync<ImageUploadDto>($"api/events/{eventId}/banner", model.BannerFile);
            if (!upload.Success)
            {
                TempData["Error"] = $"Your event was created, but the banner couldn't be uploaded: {upload.Error} You can try again below.";
                return RedirectToAction(nameof(Edit), new { id = eventId });
            }
        }

        TempData["Success"] = $"\"{created.Data.Name}\" has been created.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------ Edit

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound
                ? "That event could not be found."
                : result.Error;
            return RedirectToAction(nameof(Index));
        }

        // Organisers can only change their own events.
        if (!IsCurrentUser(result.Data.OrganiserId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(ToForm(result.Data));
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View(model);

        var updated = await _api.PutAsync<EventDto>($"api/events/{id}", BuildRequest(model));

        var redirect = HandleAuthFailure(updated);
        if (redirect != null) return redirect;

        if (updated.StatusCode == HttpStatusCode.NotFound)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Index));
        }

        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty,
                updated.Error ?? "We couldn't save your changes. Please try again.");
            return View(model);
        }

        if (model.BannerFile is { Length: > 0 })
        {
            var upload = await _api.UploadAsync<ImageUploadDto>($"api/events/{id}/banner", model.BannerFile);
            if (!upload.Success)
            {
                TempData["Error"] = $"Your changes were saved, but the banner couldn't be uploaded: {upload.Error}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        TempData["Success"] = $"\"{model.Name}\" has been updated.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------ Delete

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound
                ? "That event could not be found."
                : result.Error;
            return RedirectToAction(nameof(Index));
        }

        if (!IsCurrentUser(result.Data.OrganiserId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(result.Data);
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _api.DeleteAsync($"api/events/{id}");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (result.Success)
        {
            TempData["Success"] = "The event has been deleted.";
        }
        else
        {
            // For example: the API refuses to delete an event that still has categories.
            TempData["Error"] = result.Error ?? "We couldn't delete the event. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------ helpers

    private static object BuildRequest(EventFormViewModel m) => new
    {
        name = m.Name.Trim(),
        description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(),
        eventDate = m.EventDate!.Value.Date,
        location = m.Location.Trim(),
        distanceKm = m.DistanceKm!.Value,
        eventType = m.EventType
    };

    private static EventFormViewModel ToForm(EventDto e) => new()
    {
        EventId = e.EventId,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate.Date,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        ExistingBannerUrl = e.BannerUrl
    };
}
