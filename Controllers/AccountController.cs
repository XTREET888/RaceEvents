using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.Enums;
using RaceEvents.Models.ViewModels;
using RaceEvents.Services;

namespace RaceEvents.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Пользователь с таким email уже существует");
                return View(model);
            }

            var participant = new Participant
            {
                Email = model.Email,
                PasswordHash = PasswordHasher.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = Role.PARTICIPANT,
                DriverLicense = model.DriverLicense,
                DateOfBirth = model.DateOfBirth,
                Phone = model.Phone,
                RegistrationDate = DateTime.Now
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("UserId", participant.Id);
            HttpContext.Session.SetString("UserRole", participant.Role.ToString());
            HttpContext.Session.SetString("UserName", $"{participant.FirstName} {participant.LastName}");

            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null && PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role.ToString());
                HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");

                if (user.Role == Role.ADMINISTRATOR)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Неверный email или пароль");
        }

        return View(model);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}

