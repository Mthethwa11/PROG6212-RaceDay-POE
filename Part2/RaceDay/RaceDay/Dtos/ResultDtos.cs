using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos
{
    /// <summary>
    /// Data an Organiser sends to record a participant's result.
    /// </summary>
    public class ResultRequest
    {
        /// <summary>Finish time in seconds, for example 3120 for 52 minutes.</summary>
        [Range(1, 1000000)]
        public int FinishTimeSeconds { get; set; }

        /// <summary>Finishing position within the category, 1 for first place.</summary>
        [Range(1, 1000000)]
        public int Position { get; set; }
    }

    /// <summary>
    /// A result as returned by the API.
    /// </summary>
    public class ResultResponse
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

        /// <summary>The finish time as hours:minutes:seconds.</summary>
        public string? FinishTime => FinishTimeSeconds.HasValue
            ? TimeSpan.FromSeconds(FinishTimeSeconds.Value).ToString(@"hh\:mm\:ss")
            : null;
    }
}
