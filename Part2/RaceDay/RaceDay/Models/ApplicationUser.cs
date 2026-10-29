using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RaceDay.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        /// <summary>Public URL of the profile picture in Azure Blob Storage (null until one is uploaded).</summary>
        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
