using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The logged-in Participant's race history: finish times and positions.</summary>
[RoleAuthorize(Roles.Participant)]
[Route("my/results")]
public class MyResultsController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public MyResultsController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var results = await _api.GetAsync<List<ResultDto>>("api/users/me/results");

        var redirect = HandleAuthFailure(results);
        if (redirect != null) return redirect;

        // A result doesn't carry the event date, but the participant's enrolments do.
        var enrolments = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        redirect = HandleAuthFailure(enrolments);
        if (redirect != null) return redirect;

        var dateByEnrolment = (enrolments.Data ?? new List<EnrolmentDto>())
            .ToDictionary(e => e.EnrolmentId, e => e.EventDate);

        var rows = (results.Data ?? new List<ResultDto>())
            .Select(r => new MyResultRow
            {
                Result = r,
                EventDate = dateByEnrolment.TryGetValue(r.EnrolmentId, out var date) ? date : null
            })
            .OrderByDescending(r => r.EventDate ?? DateTime.MinValue)
            .ToList();

        return View(new MyResultsViewModel
        {
            Rows = rows,
            LoadError = results.Success ? null : results.Error
        });
    }
}
