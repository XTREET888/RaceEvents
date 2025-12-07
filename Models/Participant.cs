using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceEvents.Models;

[Table("Participants")]
public class Participant : User
{
    [Required]
    [StringLength(50)]
    public string DriverLicense { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    [DataType(DataType.Time)]
    public TimeSpan? BestLapTime { get; set; }

    [Required]
    public int PodiumCount { get; set; } = 0;

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}

