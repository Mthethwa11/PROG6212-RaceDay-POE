using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos
{
    /// <summary>
    /// Data an Organiser sends to create or update a category.
    /// </summary>
    public class CategoryRequest
    {
        /// <summary>For example "10km Race" or "Under 20".</summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 999.99)]
        public decimal DistanceKm { get; set; }

        [Range(0, 999999.99)]
        public decimal Price { get; set; }

        [Range(1, 1000000)]
        public int MaxParticipants { get; set; }
    }

    /// <summary>
    /// A category as returned by the API.
    /// </summary>
    public class CategoryResponse
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal Price { get; set; }
        public int MaxParticipants { get; set; }
    }
}
