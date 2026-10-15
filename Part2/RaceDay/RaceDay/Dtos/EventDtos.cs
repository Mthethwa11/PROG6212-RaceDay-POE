using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos
{
    /// <summary>
    /// Data an Organiser sends to create or update an event.
    /// </summary>
    public class EventRequest
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [Range(0.01, 9999.99)]
        public decimal DistanceKm { get; set; }

        /// <summary>Must be Run, Walk or Cycle.</summary>
        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
        public string EventType { get; set; } = string.Empty;
    }

    /// <summary>
    /// An event as returned by the API.
    /// </summary>
    public class EventResponse
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string OrganiserId { get; set; } = string.Empty;
        public string OrganiserName { get; set; } = string.Empty;
        public List<CategorySummary> Categories { get; set; } = new();
    }

    /// <summary>
    /// Short category details shown inside an event.
    /// </summary>
    public class CategorySummary
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal Price { get; set; }
        public int MaxParticipants { get; set; }
    }
}
