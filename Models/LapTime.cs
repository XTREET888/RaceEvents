using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceEvents.Models;

public class LapTime
{
    public int Id { get; set; }

    [Required]
    public int LapNumber { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan Time { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime RecordedAt { get; set; } = DateTime.Now;

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey("ApplicationId")]
    public virtual Application Application { get; set; } = null!;
}

