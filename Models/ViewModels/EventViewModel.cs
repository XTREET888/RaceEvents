using System.ComponentModel.DataAnnotations;
using RaceEvents.Models.Enums;

namespace RaceEvents.Models.ViewModels;

public class EventViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(200)]
    [Display(Name = "Название")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата обязательна")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Дата и время")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Место проведения обязательно")]
    [StringLength(200)]
    [Display(Name = "Место проведения")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Тип трассы обязателен")]
    [StringLength(50)]
    [Display(Name = "Тип трассы")]
    public string TrackType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Максимальное количество участников обязательно")]
    [Range(1, 1000, ErrorMessage = "Некорректное количество")]
    [Display(Name = "Максимальное количество участников")]
    public int MaxParticipants { get; set; }

    [Required]
    [Display(Name = "Тип события")]
    public EventType Type { get; set; } = EventType.TRACK_DAY;

    [Required]
    [Display(Name = "Требование к типу автомобиля")]
    public CarTypeRequirement CarTypeRequirement { get; set; } = CarTypeRequirement.ANY;

    [StringLength(50)]
    [Display(Name = "Требуемый класс автомобиля")]
    public string? RequiredCarClass { get; set; }

    [Range(0, 10000, ErrorMessage = "Некорректная мощность")]
    [Display(Name = "Максимальная мощность (л.с.)")]
    public int? MaxHorsepower { get; set; }

    [StringLength(20)]
    [Display(Name = "Требуемый тип привода")]
    public string? RequiredDriveType { get; set; }

    public int? ChampionshipId { get; set; }
}

