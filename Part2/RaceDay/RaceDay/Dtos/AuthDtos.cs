using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos
{
    /// <summary>
    /// Data needed to create a new account.
    /// </summary>
    public class RegisterRequest
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>At least 6 characters with an upper-case letter, a lower-case letter, a digit and a symbol.</summary>
        [Required, MinLength(6), MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Must be Organiser or Participant.</summary>
        [Required]
        [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data needed to log in.
    /// </summary>
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Profile details a user can change about themselves.
    /// </summary>
    public class UpdateProfileRequest
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }
}
