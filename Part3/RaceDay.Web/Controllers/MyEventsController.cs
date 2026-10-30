using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The logged-in Participant's own enrolments and their status.</summary>
[RoleAuthorize(Roles.Participant)]
[Route("my/events")]
public class MyEventsController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public MyEventsController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        var all = result.Data ?? new List<EnrolmentDto>();
        var today = DateTime.Today;

        return View(new MyEventsViewModel
        {
            Upcoming = all.Where(e => e.EventDate.Date >= today).OrderBy(e => e.EventDate).ToList(),
            Past = all.Where(e => e.EventDate.Date < today).OrderByDescending(e => e.EventDate).ToList(),
            LoadError = result.Success ? null : result.Error
        });
    }

    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.DeleteAsync($"api/enrolments/{id}");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        if (result.Success)
        {
            TempData["Success"] = "Your enrolment has been cancelled.";
        }
        else
        {
            // For example: the API won't cancel an enrolment that already has a result.
            TempData["Error"] = result.Error ?? "We couldn't cancel that enrolment. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }
}
