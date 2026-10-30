using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Category management for one of the Organiser's events. All data goes through the API.</summary>
[RoleAuthorize(Roles.Organiser)]
[Route("organiser/events/{eventId:int}/categories")]
public class OrganiserCategoriesController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public OrganiserCategoriesController(IApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------ List

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(_api, eventId);
        if (failure != null) return failure;

        // Enrolment numbers per category (so the page can show how full each one is).
        var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        var redirect = HandleAuthFailure(enrolments);
        if (redirect != null) return redirect;

        var counts = (enrolments.Data ?? new List<EnrolmentDto>())
            .Where(e => e.Status == "Confirmed")
            .GroupBy(e => e.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        var model = new CategoriesPageViewModel
        {
            Event = ev!,
            Rows = ev!.Categories
                .OrderBy(c => c.DistanceKm)
                .Select(c => new CategoryRow
                {
                    Category = c,
                    Enrolled = counts.GetValueOrDefault(c.CategoryId)
                })
                .ToList()
        };
        return View(model);
    }

    // ------------------------------------------------------------ Create

    [HttpGet("create")]
    public async Task<IActionResult> Create(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(_api, eventId);
        if (failure != null) return failure;

        ViewData["Event"] = ev;
        return View(new CategoryFormViewModel { EventId = eventId });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, CategoryFormViewModel model)
    {
        model.EventId = eventId;

        var (ev, failure) = await LoadOwnedEventAsync(_api, eventId);
        if (failure != null) return failure;
        ViewData["Event"] = ev;

        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<CategorySummaryDto>($"api/events/{eventId}/categories", BuildRequest(model));

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty,
                result.Error ?? "We couldn't add the category. Please try again.");
            return View(model);
        }

        TempData["Success"] = $"\"{model.Name}\" has been added.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    // ------------------------------------------------------------ Edit

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var (ev, failure) = await LoadOwnedEventAsync(_api, eventId);
        if (failure != null) return failure;

        var category = ev!.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category == null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        ViewData["Event"] = ev;
        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            Name = category.Name,
            DistanceKm = category.DistanceKm,
            Price = category.Price,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        model.CategoryId = id;

        var (ev, failure) = await LoadOwnedEventAsync(_api, eventId);
        if (failure != null) return failure;
        ViewData["Event"] = ev;

        if (!ModelState.IsValid) return View(model);

        var result = await _api.PutAsync<CategorySummaryDto>($"api/categories/{id}", BuildRequest(model));

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty,
                result.Error ?? "We couldn't save your changes. Please try again.");
            return View(model);
        }

        TempData["Success"] = $"\"{model.Name}\" has been updated.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    // ------------------------------------------------------------ Delete

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await _api.DeleteAsync($"api/categories/{id}");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (result.Success)
        {
            TempData["Success"] = "The category has been deleted.";
        }
        else
        {
            // For example: the API refuses to delete a category that already has enrolments.
            TempData["Error"] = result.Error ?? "We couldn't delete the category. Please try again.";
        }
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private static object BuildRequest(CategoryFormViewModel m) => new
    {
        name = m.Name.Trim(),
        distanceKm = m.DistanceKm!.Value,
        price = m.Price!.Value,
        maxParticipants = m.MaxParticipants!.Value
    };
}
