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
    /// Categories (for example "5km" or "10km") belong to an event. Anyone can view them;
    /// only the Organiser who owns the event can change them.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Lists all categories for an event.
        /// </summary>
        /// <remarks>Public. No request body.</remarks>
        /// <param name="eventId">The event ID.</param>
        /// <response code="200">An array of categories (may be empty).</response>
        /// <response code="404">No event exists with that ID.</response>
        [HttpGet("events/{eventId:int}/categories")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CategoryResponse>>> GetByEvent(int eventId)
        {
            if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            {
                return NotFound();
            }

            var categories = await _db.Categories
                .AsNoTracking()
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.DistanceKm)
                .Select(c => ToResponse(c))
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Adds a category to an event.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only the Organiser who owns the event.
        /// Body: name, distanceKm, price and maxParticipants.
        /// </remarks>
        /// <param name="eventId">The event ID.</param>
        /// <response code="201">The category was created. The new category is returned.</response>
        /// <response code="400">The request body is invalid.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own this event.</response>
        /// <response code="404">No event exists with that ID.</response>
        [HttpPost("events/{eventId:int}/categories")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryResponse>> Create(int eventId, [FromBody] CategoryRequest request)
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

            var category = new Category
            {
                EventId = eventId,
                Name = request.Name,
                DistanceKm = request.DistanceKm,
                Price = request.Price,
                MaxParticipants = request.MaxParticipants
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByEvent), new { eventId }, ToResponse(category));
        }

        /// <summary>
        /// Updates a category.
        /// </summary>
        /// <remarks>Organiser only, and only the Organiser who owns the category's event.</remarks>
        /// <param name="id">The category ID.</param>
        /// <response code="200">The updated category.</response>
        /// <response code="400">The request body is invalid.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own the event.</response>
        /// <response code="404">No category exists with that ID.</response>
        [HttpPut("categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryResponse>> Update(int id, [FromBody] CategoryRequest request)
        {
            var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            if (category.Event!.OrganiserId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            category.Name = request.Name;
            category.DistanceKm = request.DistanceKm;
            category.Price = request.Price;
            category.MaxParticipants = request.MaxParticipants;

            await _db.SaveChangesAsync();

            return Ok(ToResponse(category));
        }

        /// <summary>
        /// Deletes a category.
        /// </summary>
        /// <remarks>
        /// Organiser only, and only the Organiser who owns the category's event. The category's
        /// route is deleted with it. A category that already has enrolments cannot be deleted.
        /// </remarks>
        /// <param name="id">The category ID.</param>
        /// <response code="204">The category was deleted.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="403">The caller is not an Organiser, or does not own the event.</response>
        /// <response code="404">No category exists with that ID.</response>
        /// <response code="409">The category already has enrolments.</response>
        [HttpDelete("categories/{id:int}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Event)
                .Include(c => c.Route)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Event!.OrganiserId != _userManager.GetUserId(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            {
                return Conflict(new { message = "This category already has enrolments and cannot be deleted." });
            }

            if (category.Route != null)
            {
                _db.Routes.Remove(category.Route);
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static CategoryResponse ToResponse(Category c) => new()
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            Price = c.Price,
            MaxParticipants = c.MaxParticipants
        };
    }
}
