using System.Linq.Expressions;
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
    /// Participants enter events by enrolling in a category. Organisers can see who has
    /// enrolled in their events.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrolmentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // One place that defines how an Enrolment becomes an EnrolmentResponse.
        private static readonly Expression<Func<Enrolment, EnrolmentResponse>> ToResponse = e => new EnrolmentResponse
        {
            EnrolmentId = e.EnrolmentId,
            ParticipantId = e.ParticipantId,
            ParticipantName = e.Participant!.FullName,
            EventId = e.Category!.EventId,
            EventName = e.Category.Event!.Name,
            EventDate = e.Category.Event.EventDate,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.Name,
            EnrolmentDate = e.EnrolmentDate,
            Status = e.Status
        };

        /// <summary>
        /// Enrols the logged-in Participant in a category.
        /// </summary>
        /// <remarks>
        /// Participant only. No request body: the participant comes from the session and the
        /// category from the URL. The link between participant, event and category is recorded.
        /// </remarks>
        /// <param name="id">The category ID.</param>
        /// <response code="201">The enrolment was created and is returned.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not a Participant.</response>
        /// <response code="404">No category exists with that ID.</response>
        /// <response code="409">The participant is already enrolled, or the category is full.</response>
        [HttpPost("categories/{id:int}/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnrolmentResponse>> Enrol(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == userId && e.CategoryId == id))
            {
                return Conflict(new { message = "You are already enrolled in this category." });
            }

            var taken = await _db.Enrolments.CountAsync(e => e.CategoryId == id && e.Status == "Confirmed");
            if (taken >= category.MaxParticipants)
            {
                return Conflict(new { message = "This category is full." });
            }

            var enrolment = new Enrolment { ParticipantId = userId, CategoryId = id, Status = "Confirmed" };
            _db.Enrolments.Add(enrolment);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two requests at once can hit the unique (participant, category) rule.
                return Conflict(new { message = "You are already enrolled in this category." });
            }

            var response = await _db.Enrolments
                .AsNoTracking()
                .Where(e => e.EnrolmentId == enrolment.EnrolmentId)
                .Select(ToResponse)
                .FirstAsync();

            return CreatedAtAction(nameof(GetMine), null, response);
        }

        /// <summary>
        /// Lists the logged-in Participant's own enrolments.
        /// </summary>
        /// <remarks>Participant only. No request body. Newest enrolment first.</remarks>
        /// <response code="200">An array of the participant's enrolments (may be empty).</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not a Participant.</response>
        [HttpGet("users/me/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<EnrolmentResponse>>> GetMine()
        {
            var userId = _userManager.GetUserId(User)!;

            var list = await _db.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantId == userId)
                .OrderByDescending(e => e.EnrolmentDate)
                .Select(ToResponse)
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Lists every enrolment for an event.
        /// </summary>
        /// <remarks>Organiser only, and only the Organiser who owns the event. No request body.</remarks>
        /// <param name="eventId">The event ID.</param>
        /// <response code="200">An array of enrolments for the event (may be empty).</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own this event.</response>
        /// <response code="404">No event exists with that ID.</response>
        [HttpGet("events/{eventId:int}/enrolments")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<EnrolmentResponse>>> GetByEvent(int eventId)
        {
            var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var list = await _db.Enrolments
                .AsNoTracking()
                .Where(e => e.Category!.EventId == eventId)
                .OrderBy(e => e.Category!.Name)
                .ThenBy(e => e.EnrolmentDate)
                .Select(ToResponse)
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Cancels (deletes) an enrolment.
        /// </summary>
        /// <remarks>
        /// Participant only, and only the Participant who made the enrolment. An enrolment that
        /// already has a result cannot be cancelled.
        /// </remarks>
        /// <param name="id">The enrolment ID.</param>
        /// <response code="204">The enrolment was deleted.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not a Participant, or this is not their enrolment.</response>
        /// <response code="404">No enrolment exists with that ID.</response>
        /// <response code="409">The enrolment already has a result.</response>
        [HttpDelete("enrolments/{id:int}")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(int id)
        {
            var enrolment = await _db.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentId == id);
            if (enrolment == null)
            {
                return NotFound();
            }

            if (enrolment.ParticipantId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (enrolment.Result != null)
            {
                return Conflict(new { message = "This enrolment already has a result and cannot be cancelled." });
            }

            _db.Enrolments.Remove(enrolment);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
