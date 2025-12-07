using System.ComponentModel.DataAnnotations;

namespace RaceEvents.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, ErrorMessage = "Пароль должен быть от {2} до {1} символов", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имя обязательно")]
    [StringLength(100)]
    [Display(Name = "Имя")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Фамилия обязательна")]
    [StringLength(100)]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Номер водительского удостоверения обязателен")]
    [StringLength(50)]
    [Display(Name = "Номер водительского удостоверения")]
    public string DriverLicense { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата рождения обязательна")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата рождения")]
    public DateTime DateOfBirth { get; set; }

    [StringLength(20)]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }
}

