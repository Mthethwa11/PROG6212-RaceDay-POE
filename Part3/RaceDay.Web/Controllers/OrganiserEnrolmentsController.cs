using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shows the Organiser everyone who has enrolled in one of their events.</summary>
[RoleAuthorize(Roles.Organiser)]
[Route("organiser/events/{eventId:int}/enrolments")]
public class OrganiserEnrolmentsController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public OrganiserEnrolmentsController(IApiClient api)
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

        return View(new EnrolmentsPageViewModel
        {
            Event = ev!,
            Enrolments = enrolments.Data ?? new List<EnrolmentDto>(),
            LoadError = enrolments.Success ? null : enrolments.Error
        });
    }
}
