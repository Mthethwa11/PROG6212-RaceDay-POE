using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RaceDay.Web.Models;

/// <summary>The profile page: the editable details plus read-only account information.</summary>
public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."),
     MaxLength(100, ErrorMessage = "Your name can be at most 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address."),
     EmailAddress(ErrorMessage = "That doesn't look like a valid email address."),
     MaxLength(150, ErrorMessage = "Your email can be at most 150 characters.")]
    public string Email { get; set; } = string.Empty;

    // Display-only. Never taken from the form; always loaded from the API.
    [BindNever] public string? ProfilePictureUrl { get; set; }
    [BindNever] public string? Role { get; set; }
    [BindNever] public DateTime? CreatedAt { get; set; }
}
