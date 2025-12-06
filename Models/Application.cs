using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RaceEvents.Models.Enums;

namespace RaceEvents.Models;

public class Application
{
    public int Id { get; set; }

    [Required]
    public ApplicationStatus Status { get; set; } = ApplicationStatus.PENDING;

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime ApplicationDate { get; set; } = DateTime.Now;

    [Required]
    public int ParticipantId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public int CarId { get; set; }

    [ForeignKey("ParticipantId")]
    public virtual Participant Participant { get; set; } = null!;

    [ForeignKey("EventId")]
    public virtual Event Event { get; set; } = null!;

    [ForeignKey("CarId")]
    public virtual Car Car { get; set; } = null!;

    public virtual ICollection<LapTime> LapTimes { get; set; } = new List<LapTime>();
    public virtual FinalResult? FinalResult { get; set; }
}

