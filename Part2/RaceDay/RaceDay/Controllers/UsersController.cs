using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    /// <summary>
    /// Profile endpoints for the logged-in user (both roles).
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Returns the logged-in user's profile.
        /// </summary>
        /// <remarks>
        /// No request body. The user ID and role are read from the server-side session
        /// that is created when the user logs in.
        /// </remarks>
        /// <response code="200">The profile of the logged-in user.</response>
        /// <response code="401">The caller is not logged in.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMe()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new UserProfileDto
            {
                UserId = HttpContext.Session.GetString("UserId") ?? user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = HttpContext.Session.GetString("Role") ?? string.Empty,
                CreatedAt = user.CreatedAt
            });
        }
    }

    /// <summary>
    /// Profile data returned to the client. The password hash is never included.
    /// </summary>
    public class UserProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
