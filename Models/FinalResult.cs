using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceEvents.Models;

public class FinalResult
{
    public int Id { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan BestLapTime { get; set; }

    [Required]
    public int Position { get; set; }

    [Required]
    public int TotalLaps { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan AverageLapTime { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan TotalTime { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey("ApplicationId")]
    public virtual Application Application { get; set; } = null!;
}

