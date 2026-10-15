namespace RaceDay.Dtos
{
    /// <summary>
    /// An enrolment as returned by the API: who entered, which event and which category.
    /// </summary>
    public class EnrolmentResponse
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
}
