using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models
{
    // Named RaceRoute in code to avoid clashing with ASP.NET's [Route] attribute.
    // It maps to the "Routes" table from the Part 1 ERD.
    [Table("Routes")]
    public class RaceRoute
    {
        [Key]
        public int RouteId { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required, MaxLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? MapUrl { get; set; }

        public int? ElevationGainM { get; set; }

        public string? Description { get; set; }
    }
}
