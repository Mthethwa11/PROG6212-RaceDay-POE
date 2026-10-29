using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    /// <summary>
    /// Create, view, update and delete events. Anyone can view events; only Organisers can change them.
    /// </summary>
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Lists all upcoming events, soonest first.
        /// </summary>
        /// <remarks>Public. No request body.</remarks>
        /// <response code="200">An array of upcoming events (may be empty).</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EventResponse>>> GetAll()
        {
            var today = DateTime.Today;
            var events = await _db.Events
                .AsNoTracking()
                .Where(e => e.EventDate >= today)
                .OrderBy(e => e.EventDate)
                .Select(e => new EventResponse
                {
                    EventId = e.EventId,
                    Name = e.Name,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    DistanceKm = e.DistanceKm,
                    EventType = e.EventType,
                    OrganiserId = e.OrganiserId,
                    OrganiserName = e.Organiser!.FullName,
                    BannerUrl = e.BannerUrl
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Lists every event created by the logged-in Organiser, including past events,
        /// with the number of confirmed enrolments for each. Newest first.
        /// </summary>
        /// <remarks>Organiser only. No request body.</remarks>
        /// <response code="200">An array of the Organiser's events (may be empty).</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is logged in but is not an Organiser.</response>
        [HttpGet("mine")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<EventResponse>>> GetMine()
        {
            var userId = _userManager.GetUserId(User)!;

            var events = await _db.Events
                .AsNoTracking()
                .Where(e => e.OrganiserId == userId)
                .OrderByDescending(e => e.EventDate)
                .Select(e => new EventResponse
                {
                    EventId = e.EventId,
                    Name = e.Name,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    DistanceKm = e.DistanceKm,
                    EventType = e.EventType,
                    OrganiserId = e.OrganiserId,
                    OrganiserName = e.Organiser!.FullName,
                    BannerUrl = e.BannerUrl,
                    EnrolmentCount = e.Categories
                        .SelectMany(c => c.Enrolments)
                        .Count(en => en.Status == "Confirmed")
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Gets one event, including its categories.
        /// </summary>
        /// <remarks>Public. No request body.</remarks>
        /// <param name="id">The event ID.</param>
        /// <response code="200">The event and its categories.</response>
        /// <response code="404">No event exists with that ID.</response>
        [HttpGet("{id:int}", Name = "GetEventById")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventResponse>> GetById(int id)
        {
            var ev = await _db.Events
                .AsNoTracking()
                .Where(e => e.EventId == id)
                .Select(e => new EventResponse
                {
                    EventId = e.EventId,
                    Name = e.Name,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    DistanceKm = e.DistanceKm,
                    EventType = e.EventType,
                    OrganiserId = e.OrganiserId,
                    OrganiserName = e.Organiser!.FullName,
                    BannerUrl = e.BannerUrl,
                    EnrolmentCount = e.Categories
                        .SelectMany(c => c.Enrolments)
                        .Count(en => en.Status == "Confirmed"),
                    Categories = e.Categories.Select(c => new CategorySummary
                    {
                        CategoryId = c.CategoryId,
                        Name = c.Name,
                        DistanceKm = c.DistanceKm,
                        Price = c.Price,
                        MaxParticipants = c.MaxParticipants
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (ev == null)
            {
                return NotFound();
            }

            return Ok(ev);
        }

        /// <summary>
        /// Creates a new event owned by the logged-in Organiser.
        /// </summary>
        /// <remarks>
        /// Organiser only. Body: name, description, eventDate, location, distanceKm and
        /// eventType (Run, Walk or Cycle).
        /// </remarks>
        /// <response code="201">The event was created. The new event is returned.</response>
        /// <response code="400">The request body is missing or invalid.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is logged in but is not an Organiser.</response>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<EventResponse>> Create([FromBody] EventRequest request)
        {
            var userId = _userManager.GetUserId(User)!;

            var ev = new Event
            {
                OrganiserId = userId,
                Name = request.Name,
                Description = request.Description,
                EventDate = request.EventDate.Date,
                Location = request.Location,
                DistanceKm = request.DistanceKm,
                EventType = request.EventType
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            var organiser = await _userManager.FindByIdAsync(userId);
            var response = ToResponse(ev, organiser?.FullName ?? string.Empty);

            return CreatedAtRoute("GetEventById", new { id = ev.EventId }, response);
        }

        /// <summary>
        /// Updates an event's details.
        /// </summary>
        /// <remarks>Organiser only, and only the Organiser who created the event.</remarks>
        /// <param name="id">The event ID.</param>
        /// <response code="200">The updated event.</response>
        /// <response code="400">The request body is invalid.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own this event.</response>
        /// <response code="404">No event exists with that ID.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventResponse>> Update(int id, [FromBody] EventRequest request)
        {
            var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            ev.Name = request.Name;
            ev.Description = request.Description;
            ev.EventDate = request.EventDate.Date;
            ev.Location = request.Location;
            ev.DistanceKm = request.DistanceKm;
            ev.EventType = request.EventType;

            await _db.SaveChangesAsync();

            return Ok(ToResponse(ev, ev.Organiser?.FullName ?? string.Empty));
        }

        /// <summary>
        /// Deletes an event.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only the Organiser who created the event. An event that still
        /// has categories cannot be deleted; remove its categories first.
        /// </remarks>
        /// <param name="id">The event ID.</param>
        /// <response code="204">The event was deleted.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own this event.</response>
        /// <response code="404">No event exists with that ID.</response>
        /// <response code="409">The event still has categories.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (await _db.Categories.AnyAsync(c => c.EventId == id))
            {
                return Conflict(new { message = "This event still has categories. Delete them first." });
            }

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static EventResponse ToResponse(Event ev, string organiserName) => new()
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            OrganiserId = ev.OrganiserId,
            OrganiserName = organiserName,
            BannerUrl = ev.BannerUrl
        };
    }
}
