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
    /// Finish times and positions. Organisers record results for their own events;
    /// Participants can view their own results.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ResultsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // One place that defines how a Result becomes a ResultResponse.
        private static readonly Expression<Func<Result, ResultResponse>> ToResponse = r => new ResultResponse
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            ParticipantId = r.Enrolment!.ParticipantId,
            ParticipantName = r.Enrolment.Participant!.FullName,
            EventId = r.Enrolment.Category!.EventId,
            EventName = r.Enrolment.Category.Event!.Name,
            CategoryId = r.Enrolment.CategoryId,
            CategoryName = r.Enrolment.Category.Name,
            FinishTimeSeconds = r.FinishTimeSeconds,
            Position = r.Position,
            RecordedAt = r.RecordedAt
        };

        /// <summary>
        /// Records the finish time and position for an enrolment.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only the Organiser who owns the event. Body: finishTimeSeconds
        /// and position. Each enrolment can have only one result.
        /// </remarks>
        /// <param name="id">The enrolment ID.</param>
        /// <response code="201">The result was recorded and is returned.</response>
        /// <response code="400">The request body is invalid.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own this event.</response>
        /// <response code="404">No enrolment exists with that ID.</response>
        /// <response code="409">This enrolment already has a result.</response>
        [HttpPost("enrolments/{id:int}/result")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultResponse>> Record(int id, [FromBody] ResultRequest request)
        {
            var enrolment = await _db.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound();
            }

            if (enrolment.Category!.Event!.OrganiserId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (enrolment.Result != null)
            {
                return Conflict(new { message = "This enrolment already has a result." });
            }

            var result = new Result
            {
                EnrolmentId = id,
                FinishTimeSeconds = request.FinishTimeSeconds,
                Position = request.Position
            };

            _db.Results.Add(result);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "This enrolment already has a result." });
            }

            var response = await _db.Results
                .AsNoTracking()
                .Where(r => r.ResultId == result.ResultId)
                .Select(ToResponse)
                .FirstAsync();

            return CreatedAtAction(nameof(GetForEnrolment), new { id }, response);
        }

        /// <summary>
        /// Gets the result for one enrolment.
        /// </summary>
        /// <remarks>
        /// Any logged-in user, but only the Participant who made the enrolment or the Organiser
        /// who owns the event can see it. No request body.
        /// </remarks>
        /// <param name="id">The enrolment ID.</param>
        /// <response code="200">The result.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is neither the participant nor the event's Organiser.</response>
        /// <response code="404">The enrolment does not exist, or has no result yet.</response>
        [HttpGet("enrolments/{id:int}/result")]
        [Authorize]
        [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultResponse>> GetForEnrolment(int id)
        {
            var enrolment = await _db.Enrolments
                .AsNoTracking()
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var isParticipant = enrolment.ParticipantId == userId;
            var isOrganiser = enrolment.Category!.Event!.OrganiserId == userId;

            if (!isParticipant && !isOrganiser)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var result = await _db.Results
                .AsNoTracking()
                .Where(r => r.EnrolmentId == id)
                .Select(ToResponse)
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Lists the logged-in Participant's own results.
        /// </summary>
        /// <remarks>Participant only. No request body. Most recent result first.</remarks>
        /// <response code="200">An array of the participant's results (may be empty).</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not a Participant.</response>
        [HttpGet("users/me/results")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ResultResponse>>> GetMine()
        {
            var userId = _userManager.GetUserId(User)!;

            var list = await _db.Results
                .AsNoTracking()
                .Where(r => r.Enrolment!.ParticipantId == userId)
                .OrderByDescending(r => r.RecordedAt)
                .Select(ToResponse)
                .ToListAsync();

            return Ok(list);
        }
    }
}
