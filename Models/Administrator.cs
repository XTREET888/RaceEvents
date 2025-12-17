using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceEvents.Models;

public class Administrator : User
{
    [StringLength(100)]
    public string? Department { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime HireDate { get; set; } = DateTime.Now;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    public virtual ICollection<Championship> Championships { get; set; } = new List<Championship>();
}

