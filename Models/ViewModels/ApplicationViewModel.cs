using System.ComponentModel.DataAnnotations;
using RaceEvents.Models.Enums;

namespace RaceEvents.Models.ViewModels;

public class ApplicationViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Событие обязательно")]
    [Display(Name = "Событие")]
    public int EventId { get; set; }

    [Required(ErrorMessage = "Автомобиль обязателен")]
    [Display(Name = "Автомобиль")]
    public int CarId { get; set; }

    [Required(ErrorMessage = "Тип шлема обязателен")]
    [Display(Name = "Тип шлема")]
    public HelmetType HelmetType { get; set; } = HelmetType.OWN;

    [Required(ErrorMessage = "Тип счетчика времени обязателен")]
    [Display(Name = "Счетчик времени")]
    public TimerType TimerType { get; set; } = TimerType.NONE;
}

