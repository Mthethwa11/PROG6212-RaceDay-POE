using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Results entry: the Organiser records finish times and positions after an event.</summary>
[RoleAuthorize(Roles.Organiser)]
[Route("organiser/events/{eventId:int}/results")]
public class OrganiserResultsController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public OrganiserResultsController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(_api, eventId);
        if (failure != null) return failure;

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        var redirect = HandleAuthFailure(enrolments);
        if (redirect != null) return redirect;

        var results = await _api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");
        redirect = HandleAuthFailure(results);
        if (redirect != null) return redirect;

        var byEnrolment = (results.Data ?? new List<ResultDto>()).ToDictionary(r => r.EnrolmentId);

        var model = new ResultsPageViewModel
        {
            Event = ev!,
            CanEnterResults = ev!.EventDate.Date <= DateTime.Today,
            LoadError = !enrolments.Success ? enrolments.Error : !results.Success ? results.Error : null,
            Rows = (enrolments.Data ?? new List<EnrolmentDto>())
                .Select(e => new ResultRow
                {
                    Enrolment = e,
                    Result = byEnrolment.GetValueOrDefault(e.EnrolmentId)
                })
                .ToList()
        };
        return View(model);
    }

    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int eventId, ResultEntryModel model)
    {
        var (ev, failure) = await LoadOwnedEventAsync(_api, eventId);
        if (failure != null) return failure;

        if (ev!.EventDate.Date > DateTime.Today)
        {
            TempData["Error"] = "Results can be entered once the event has taken place.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        // Turn the hours / minutes / seconds boxes into the single number of seconds the API stores.
        var hours = model.Hours ?? 0;
        var minutes = model.Minutes ?? 0;
        var seconds = model.Seconds ?? 0;

        if (hours is < 0 or > 99 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
        {
            return Back(eventId, model.EnrolmentId, "Hours can be 0 to 99, and minutes and seconds 0 to 59.");
        }

        var totalSeconds = hours * 3600 + minutes * 60 + seconds;
        if (totalSeconds < 1)
        {
            return Back(eventId, model.EnrolmentId, "Please enter a finish time.");
        }

        if (model.Position is null or < 1)
        {
            return Back(eventId, model.EnrolmentId, "Please enter the finishing position (1 for first place).");
        }

        var body = new { finishTimeSeconds = totalSeconds, position = model.Position.Value };
        var path = $"api/enrolments/{model.EnrolmentId}/result";

        var result = model.HasResult
            ? await _api.PutAsync<ResultDto>(path, body)
            : await _api.PostAsync<ResultDto>(path, body);

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (!result.Success)
        {
            return Back(eventId, model.EnrolmentId,
                result.Error ?? "We couldn't save that result. Please try again.");
        }

        TempData["Success"] = model.HasResult ? "The result has been updated." : "The result has been saved.";
        return RedirectToAction(nameof(Index), "OrganiserResults", new { eventId }, $"row-{model.EnrolmentId}");
    }

    private IActionResult Back(int eventId, int enrolmentId, string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index), "OrganiserResults", new { eventId }, $"row-{enrolmentId}");
    }
}
