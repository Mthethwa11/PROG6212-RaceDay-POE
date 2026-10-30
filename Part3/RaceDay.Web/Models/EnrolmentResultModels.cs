namespace RaceDay.Web.Models;

/// <summary>An enrolment as returned by the API.</summary>
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public string ParticipantId { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>A result as returned by the API.</summary>
public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantId { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? FinishTimeSeconds { get; set; }
    public int? Position { get; set; }
    public DateTime RecordedAt { get; set; }
    public int TotalFinishers { get; set; }

    public int Hours => (FinishTimeSeconds ?? 0) / 3600;
    public int Minutes => (FinishTimeSeconds ?? 0) % 3600 / 60;
    public int Seconds => (FinishTimeSeconds ?? 0) % 60;

    public string FinishTimeText =>
        FinishTimeSeconds.HasValue ? $"{Hours:00}:{Minutes:00}:{Seconds:00}" : "-";

    /// <summary>For example "47th of 312".</summary>
    public string PositionText =>
        Position.HasValue ? $"{Ordinal.Of(Position.Value)} of {TotalFinishers}" : "-";
}

public static class Ordinal
{
    public static string Of(int n)
    {
        var lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13) return $"{n}th";

        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }
}

public class EnrolmentsPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public string? LoadError { get; set; }
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsPageViewModel
{
    public EventDto Event { get; set; } = new();
    public bool CanEnterResults { get; set; }
    public List<ResultRow> Rows { get; set; } = new();
    public string? LoadError { get; set; }

    public IEnumerable<IGrouping<string, ResultRow>> ByCategory =>
        Rows.GroupBy(r => r.Enrolment.CategoryName);
}

/// <summary>One row of the results form.</summary>
public class ResultEntryModel
{
    public int EnrolmentId { get; set; }
    public bool HasResult { get; set; }
    public int? Hours { get; set; }
    public int? Minutes { get; set; }
    public int? Seconds { get; set; }
    public int? Position { get; set; }
}
