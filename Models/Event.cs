using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RaceEvents.Models.Enums;

namespace RaceEvents.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string TrackType { get; set; } = string.Empty;

    [Required]
    public int MaxParticipants { get; set; }

    [Required]
    public EventStatus Status { get; set; } = EventStatus.UPCOMING;

    [Required]
    public int AdministratorId { get; set; }

    [ForeignKey("AdministratorId")]
    public virtual Administrator Administrator { get; set; } = null!;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}

