using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceEvents.Models;

public class Car
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string CarClass { get; set; } = string.Empty;

    [Required]
    public int Year { get; set; }

    [StringLength(30)]
    public string? Color { get; set; }

    [Required]
    [StringLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    public int ParticipantId { get; set; }

    [ForeignKey("ParticipantId")]
    public virtual Participant Participant { get; set; } = null!;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}

