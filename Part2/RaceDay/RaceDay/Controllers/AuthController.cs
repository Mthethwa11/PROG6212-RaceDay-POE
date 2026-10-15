using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    /// <summary>
    /// Register, log in and log out. Logging in creates the server-side session that holds
    /// the user's ID and role.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Registers a new user as an Organiser or a Participant.
        /// </summary>
        /// <remarks>
        /// Public. Body: fullName, email, password and role (Organiser or Participant).
        /// The password is hashed before it is stored. The user is not logged in automatically.
        /// </remarks>
        /// <response code="201">The account was created. The user is returned (no password).</response>
        /// <response code="400">The request is invalid, or the password does not meet the rules.</response>
        /// <response code="409">An account with this email already exists.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
                {
                    return Conflict(new { message = "An account with this email already exists." });
                }

                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            await _userManager.AddToRoleAsync(user, request.Role);

            var dto = new UserProfileDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = request.Role,
                CreatedAt = user.CreatedAt
            };

            return Created("/api/users/me", dto);
        }

        /// <summary>
        /// Logs a user in.
        /// </summary>
        /// <remarks>
        /// Public. Body: email and password. On success a login cookie is issued and the
        /// user's ID and role are stored in the server-side session for all later requests.
        /// </remarks>
        /// <response code="200">Logged in. The user is returned.</response>
        /// <response code="400">The request body is invalid.</response>
        /// <response code="401">The email or password is wrong.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, request.Password, isPersistent: false, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;

            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Role", role);

            return Ok(new UserProfileDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = role,
                CreatedAt = user.CreatedAt
            });
        }

        /// <summary>
        /// Logs the current user out and clears the session.
        /// </summary>
        /// <remarks>Any logged-in user. No request body.</remarks>
        /// <response code="204">Logged out.</response>
        /// <response code="401">The caller is not logged in.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return NoContent();
        }
    }
}
