using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Models;

public static class EventTypes
{
    public static readonly string[] All = { "Run", "Walk", "Cycle" };
}

/// <summary>A category as returned inside an event by the API.</summary>
public class CategorySummaryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal Price { get; set; }
    public int MaxParticipants { get; set; }
}

/// <summary>An event as returned by the API (same shape as the API's EventResponse).</summary>
public class EventDto
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
    public string? BannerUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategorySummaryDto> Categories { get; set; } = new();

    public bool IsUpcoming => EventDate.Date >= DateTime.Today;

    public string WhenText
    {
        get
        {
            var days = (EventDate.Date - DateTime.Today).Days;
            return days switch
            {
                0 => "Today",
                1 => "Tomorrow",
                > 1 => $"In {days} days",
                -1 => "Yesterday",
                _ => $"{-days} days ago"
            };
        }
    }
}

public class ImageUploadDto
{
    public string Url { get; set; } = string.Empty;
}

/// <summary>The create / edit event form.</summary>
public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."),
     MaxLength(150, ErrorMessage = "The name can be at most 150 characters.")]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."),
     MaxLength(150, ErrorMessage = "The location can be at most 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance."),
     Range(0.01, 9999.99, ErrorMessage = "The distance must be between 0.01 and 9999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type."),
     RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Please choose Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = string.Empty;

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    public string? ExistingBannerUrl { get; set; }
}

public class OrganiserDashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? LoadError { get; set; }

    public IEnumerable<EventDto> Upcoming =>
        Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate);

    public IEnumerable<EventDto> Past =>
        Events.Where(e => !e.IsUpcoming).OrderByDescending(e => e.EventDate);

    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
}
