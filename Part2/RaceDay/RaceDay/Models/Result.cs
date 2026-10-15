namespace RaceDay.Models
{
    public class Result
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }
        public Enrolment? Enrolment { get; set; }

        public int? FinishTimeSeconds { get; set; }

        public int? Position { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}
