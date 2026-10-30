using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name."),
     MaxLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance."),
     Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please enter the price (use 0 for free)."),
     Range(0, 999999.99, ErrorMessage = "The price must be 0 or more.")]
    [Display(Name = "Price (R)")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Please enter the maximum number of participants."),
     Range(1, 1000000, ErrorMessage = "There must be room for at least 1 participant.")]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }
}

public class CategoryRow
{
    public CategorySummaryDto Category { get; set; } = new();
    public int Enrolled { get; set; }
}

public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryRow> Rows { get; set; } = new();
}
