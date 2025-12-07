using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RaceEvents.Models.Enums;

namespace RaceEvents.Models;

public class Championship
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(50)]
    public string RequiredCarClass { get; set; } = string.Empty;

    [Required]
    public int MinPodiumsRequired { get; set; } = 3;

    [Required]
    public int AdministratorId { get; set; }

    [ForeignKey("AdministratorId")]
    public virtual Administrator Administrator { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

