using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class Enrolment
    {
        public int EnrolmentId { get; set; }

        [Required]
        public string ParticipantId { get; set; } = string.Empty;
        public ApplicationUser? Participant { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.Now;

        // Confirmed or Cancelled (enforced by a CHECK constraint)
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Confirmed";

        public Result? Result { get; set; }
    }
}
