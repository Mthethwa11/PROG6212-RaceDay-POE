using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize(Roles.Organiser)]
public class OrganiserController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public OrganiserController(IApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Dashboard()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");

        var redirect = HandleAuthFailure(result);
        if (redirect != null) return redirect;

        var model = new OrganiserDashboardViewModel
        {
            Events = result.Data ?? new List<EventDto>(),
            LoadError = result.Success ? null : result.Error
        };
        return View(model);
    }
}
