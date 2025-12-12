using System.ComponentModel.DataAnnotations;

namespace RaceEvents.Models.ViewModels;

public class CarViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Марка обязательна")]
    [StringLength(50)]
    [Display(Name = "Марка")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Модель обязательна")]
    [StringLength(50)]
    [Display(Name = "Модель")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Класс обязателен")]
    [StringLength(50)]
    [Display(Name = "Класс")]
    public string CarClass { get; set; } = string.Empty;

    [Required(ErrorMessage = "Год обязателен")]
    [Range(1900, 2100, ErrorMessage = "Некорректный год")]
    [Display(Name = "Год")]
    public int Year { get; set; }

    [StringLength(30)]
    [Display(Name = "Цвет")]
    public string? Color { get; set; }

    [Required(ErrorMessage = "Госномер обязателен")]
    [StringLength(20)]
    [Display(Name = "Госномер")]
    public string LicensePlate { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "Некорректная мощность")]
    [Display(Name = "Мощность (л.с.)")]
    public int? Horsepower { get; set; }

    [StringLength(20)]
    [Display(Name = "Тип привода")]
    public string? DriveType { get; set; }
}

