namespace RaceDay.Web.Models;

public class EventFilter
{
    public string? Q { get; set; }
    public string? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Q) || !string.IsNullOrWhiteSpace(Type) || From.HasValue || To.HasValue;
}

public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalUpcoming { get; set; }
    public EventFilter Filter { get; set; } = new();
    public string? LoadError { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();

    /// <summary>Categories of this event the logged-in participant is already enrolled in.</summary>
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();

    public bool IsGuest { get; set; }
    public bool IsParticipant { get; set; }
    public bool IsPast => Event.EventDate.Date < DateTime.Today;
}

public class MyEventsViewModel
{
    public List<EnrolmentDto> Upcoming { get; set; } = new();
    public List<EnrolmentDto> Past { get; set; } = new();
    public string? LoadError { get; set; }
}

public class MyResultRow
{
    public ResultDto Result { get; set; } = new();
    public DateTime? EventDate { get; set; }
}

public class MyResultsViewModel
{
    public List<MyResultRow> Rows { get; set; } = new();
    public string? LoadError { get; set; }
}
