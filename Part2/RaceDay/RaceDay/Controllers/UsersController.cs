using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Dtos;
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
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UsersController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

            return Ok(ToDto(user));
        }

        /// <summary>
        /// Updates the logged-in user's name and email.
        /// </summary>
        /// <remarks>Body: fullName and email. The role cannot be changed here.</remarks>
        /// <response code="200">The updated profile.</response>
        /// <response code="400">The request body is invalid.</response>
        /// <response code="401">The caller is not logged in.</response>
        /// <response code="409">Another account already uses this email.</response>
        [HttpPut("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.UserName = request.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
                {
                    return Conflict(new { message = "Another account already uses this email." });
                }

                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            await _signInManager.RefreshSignInAsync(user);

            return Ok(ToDto(user));
        }

        private UserProfileDto ToDto(ApplicationUser user) => new()
        {
            UserId = HttpContext.Session.GetString("UserId") ?? user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = HttpContext.Session.GetString("Role") ?? string.Empty,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt
        };
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

        /// <summary>Public URL of the profile picture in Azure Blob Storage, or null if none has been uploaded.</summary>
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
