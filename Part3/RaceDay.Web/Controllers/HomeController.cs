using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : RaceDayControllerBase
{
    private readonly IApiClient _api;

    public HomeController(IApiClient api)
    {
        _api = api;
    }

    /// <summary>Upcoming events with filters. Anyone can browse; entering an event needs a Participant login.</summary>
    public async Task<IActionResult> Index(
        string? q,
        string? type,
        [FromQuery(Name = "from")] DateTime? dateFrom,
        [FromQuery(Name = "to")] DateTime? dateTo)
    {
        // Organisers have their own dashboard instead of the public/participant home page.
        if (HttpContext.Session.IsOrganiser())
        {
            return RedirectToAction("Dashboard", "Organiser");
        }

        var result = await _api.GetAsync<List<EventDto>>("api/events");
        var all = result.Data ?? new List<EventDto>();

        IEnumerable<EventDto> filtered = all;

        var search = q?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(type) && EventTypes.All.Contains(type))
        {
            filtered = filtered.Where(e => e.EventType == type);
        }

        if (dateFrom.HasValue)
        {
            filtered = filtered.Where(e => e.EventDate.Date >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            filtered = filtered.Where(e => e.EventDate.Date <= dateTo.Value.Date);
        }

        return View(new HomeViewModel
        {
            Events = filtered.OrderBy(e => e.EventDate).ToList(),
            TotalUpcoming = all.Count,
            Filter = new EventFilter { Q = search, Type = type, From = dateFrom, To = dateTo },
            LoadError = result.Success ? null : result.Error
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
