using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required]
        public string OrganiserId { get; set; } = string.Empty;
        public ApplicationUser? Organiser { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "date")]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [Column(TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        // Run, Walk or Cycle (enforced by a CHECK constraint)
        [Required, MaxLength(10)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>Public URL of the banner image in Azure Blob Storage (null until one is uploaded).</summary>
        [MaxLength(500)]
        public string? BannerUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
