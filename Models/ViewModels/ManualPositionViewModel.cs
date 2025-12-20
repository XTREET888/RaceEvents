using System.ComponentModel.DataAnnotations;

namespace RaceEvents.Models.ViewModels;

public class ManualPositionViewModel
{
    public int ApplicationId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CarInfo { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Место обязательно")]
    [Range(1, 1000, ErrorMessage = "Место должно быть от 1 до 1000")]
    [Display(Name = "Место")]
    public int Position { get; set; }
}

public class EventManualPositionsViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public List<ManualPositionViewModel> Positions { get; set; } = new List<ManualPositionViewModel>();
}

