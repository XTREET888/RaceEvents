using System.ComponentModel.DataAnnotations;

namespace RaceEvents.Models.ViewModels;

public class LapTimeViewModel
{
    [Required(ErrorMessage = "Событие обязательно")]
    [Display(Name = "Событие")]
    public int EventId { get; set; }

    [Required(ErrorMessage = "Участник обязателен")]
    [Display(Name = "Участник")]
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "Номер круга обязателен")]
    [Display(Name = "Номер круга")]
    [Range(1, 100, ErrorMessage = "Номер круга должен быть от 1 до 100")]
    public int LapNumber { get; set; }

    [Required(ErrorMessage = "Время круга обязательно")]
    [Display(Name = "Время круга")]
    public string Time { get; set; } = string.Empty;
}

