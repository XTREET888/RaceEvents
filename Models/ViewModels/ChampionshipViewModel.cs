using System.ComponentModel.DataAnnotations;

namespace RaceEvents.Models.ViewModels;

public class ChampionshipViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(200)]
    [Display(Name = "Название")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата начала обязательна")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Дата начала")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Дата окончания обязательна")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Дата окончания")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Требуемый класс автомобиля обязателен")]
    [StringLength(50)]
    [Display(Name = "Требуемый класс автомобиля")]
    public string RequiredCarClass { get; set; } = string.Empty;

    [Required(ErrorMessage = "Минимальное количество подиумов обязательно")]
    [Range(1, 100, ErrorMessage = "Минимальное количество подиумов должно быть от 1 до 100")]
    [Display(Name = "Минимальное количество подиумов")]
    public int MinPodiumsRequired { get; set; } = 3;
}

