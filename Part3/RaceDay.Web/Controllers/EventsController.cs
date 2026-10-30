using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Event details and enrolling, for Participants (and guests who are just looking).</summary>
[Route("events")]
public class EventsController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public EventsController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");

        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound
                ? "That event could not be found."
                : result.Error;
            return RedirectToAction("Index", "Home");
        }

        var session = HttpContext.Session;
        var enrolledCategoryIds = new HashSet<int>();

        if (session.IsParticipant())
        {
            // Find out which of this event's categories the participant has already entered.
            var mine = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");

            var redirect = HandleAuthFailure(mine);
            if (redirect != null) return redirect;

            if (mine.Success && mine.Data is not null)
            {
                foreach (var enrolment in mine.Data.Where(e => e.EventId == id))
                {
                    enrolledCategoryIds.Add(enrolment.CategoryId);
                }
            }
        }

        return View(new EventDetailsViewModel
        {
            Event = result.Data,
            EnrolledCategoryIds = enrolledCategoryIds,
            IsGuest = !session.IsAuthenticated(),
            IsParticipant = session.IsParticipant()
        });
    }

    [HttpPost("{id:int}/enrol/{categoryId:int}")]
    [RoleAuthorize(Roles.Participant)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, int categoryId)
    {
        // Recorded in the system immediately by the API.
        var result = await _api.PostAsync<EnrolmentDto>($"api/categories/{categoryId}/enrolments");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (!result.Success)
        {
            // For example: already enrolled, or the category is full.
            TempData["Error"] = result.Error ?? "We couldn't enrol you. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var data = result.Data;
        TempData["Success"] = data is null
            ? "You're in! Your enrolment has been recorded."
            : $"You're in! You've entered {data.CategoryName} at {data.EventName}.";
        return RedirectToAction("Index", "MyEvents");
    }
}
