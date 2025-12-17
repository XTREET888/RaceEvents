using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.Enums;
using RaceEvents.Models.ViewModels;

namespace RaceEvents.Controllers;

public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ApplicationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "PARTICIPANT")
        {
            return RedirectToAction("Login", "Account");
        }

        var applications = await _context.Applications
            .Include(a => a.Event)
            .Include(a => a.Car)
            .Where(a => a.ParticipantId == userId.Value)
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();

        return View(applications);
    }

    public async Task<IActionResult> Details(int? id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "PARTICIPANT")
        {
            return RedirectToAction("Login", "Account");
        }

        if (id == null)
        {
            return NotFound();
        }

        var application = await _context.Applications
            .Include(a => a.Event)
            .Include(a => a.Car)
            .Include(a => a.Participant)
            .FirstOrDefaultAsync(a => a.Id == id && a.ParticipantId == userId.Value);

        if (application == null)
        {
            return NotFound();
        }

        return View(application);
    }

    public async Task<IActionResult> Create(int? eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "PARTICIPANT")
        {
            return RedirectToAction("Login", "Account");
        }

        var availableEvents = await _context.Events
            .Where(e => e.Status == EventStatus.REGISTRATION_OPEN)
            .OrderBy(e => e.Date)
            .ToListAsync();

        var participantCars = await _context.Cars
            .Where(c => c.ParticipantId == userId.Value)
            .ToListAsync();

        if (!participantCars.Any())
        {
            TempData["ErrorMessage"] = "У вас нет зарегистрированных автомобилей. Пожалуйста, сначала добавьте автомобиль.";
            return RedirectToAction("Index", "Cars");
        }

        ViewBag.Events = availableEvents;
        ViewBag.Cars = participantCars;

        var model = new ApplicationViewModel();
        if (eventId.HasValue && availableEvents.Any(e => e.Id == eventId.Value))
        {
            model.EventId = eventId.Value;
        }

        ViewBag.HelmetTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = HelmetType.OWN.ToString(), Text = "Свой" },
            new SelectListItem { Value = HelmetType.RENTAL.ToString(), Text = "На прокат" }
        };

        ViewBag.TimerTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = TimerType.NONE.ToString(), Text = "Нет" },
            new SelectListItem { Value = TimerType.RENTAL.ToString(), Text = "На прокат" }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApplicationViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "PARTICIPANT")
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            var eventItem = await _context.Events
                .Include(e => e.Championship)
                .FirstOrDefaultAsync(e => e.Id == model.EventId);

            if (eventItem == null)
            {
                ModelState.AddModelError("EventId", "Событие не найдено");
                return await LoadCreateViewData(model);
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.Id == model.CarId && c.ParticipantId == userId.Value);

            if (car == null)
            {
                ModelState.AddModelError("CarId", "Автомобиль не найден или не принадлежит вам");
                return await LoadCreateViewData(model);
            }

            var validationResult = ValidateCarForEvent(car, eventItem);
            if (!validationResult.IsValid)
            {
                ModelState.AddModelError("", validationResult.ErrorMessage);
                return await LoadCreateViewData(model);
            }

            if (eventItem.Championship != null)
            {
                var championshipValidation = await ValidateChampionshipRequirements(userId.Value, eventItem.Championship);
                if (!championshipValidation.IsValid)
                {
                    ModelState.AddModelError("", championshipValidation.ErrorMessage);
                    return await LoadCreateViewData(model);
                }
            }

            var approvedCount = await _context.Applications
                .CountAsync(a => a.EventId == model.EventId && a.Status == ApplicationStatus.APPROVED);

            if (approvedCount >= eventItem.MaxParticipants)
            {
                ModelState.AddModelError("", "Достигнуто максимальное количество участников для этого события");
                return await LoadCreateViewData(model);
            }

            var existingApplication = await _context.Applications
                .FirstOrDefaultAsync(a => a.ParticipantId == userId.Value && 
                                         a.EventId == model.EventId && 
                                         a.Status != ApplicationStatus.CANCELLED);

            if (existingApplication != null)
            {
                ModelState.AddModelError("", "У вас уже есть активная заявка на это событие");
                return await LoadCreateViewData(model);
            }

            var application = new Application
            {
                EventId = model.EventId,
                CarId = model.CarId,
                ParticipantId = userId.Value,
                HelmetType = model.HelmetType,
                TimerType = model.TimerType,
                Status = ApplicationStatus.PENDING,
                ApplicationDate = DateTime.Now
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return await LoadCreateViewData(model);
    }

    private async Task<IActionResult> LoadCreateViewData(ApplicationViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        var availableEvents = await _context.Events
            .Where(e => e.Status == EventStatus.REGISTRATION_OPEN)
            .OrderBy(e => e.Date)
            .ToListAsync();

        var participantCars = await _context.Cars
            .Where(c => c.ParticipantId == userId!.Value)
            .ToListAsync();

        ViewBag.Events = availableEvents;
        ViewBag.Cars = participantCars;

        ViewBag.HelmetTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = HelmetType.OWN.ToString(), Text = "Свой" },
            new SelectListItem { Value = HelmetType.RENTAL.ToString(), Text = "На прокат" }
        };

        ViewBag.TimerTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = TimerType.NONE.ToString(), Text = "Нет" },
            new SelectListItem { Value = TimerType.RENTAL.ToString(), Text = "На прокат" }
        };

        return View(model);
    }

    private (bool IsValid, string ErrorMessage) ValidateCarForEvent(Car car, Event eventItem)
    {
        if (eventItem.CarTypeRequirement == CarTypeRequirement.SPECIFIC_CLASS)
        {
            if (string.IsNullOrEmpty(eventItem.RequiredCarClass))
            {
                return (false, "В событии указано требование к классу, но класс не задан");
            }

            if (car.CarClass != eventItem.RequiredCarClass)
            {
                return (false, $"Автомобиль не соответствует требуемому классу. Требуется: {eventItem.RequiredCarClass}, у автомобиля: {car.CarClass}");
            }
        }

        if (eventItem.MaxHorsepower.HasValue && car.Horsepower.HasValue)
        {
            if (car.Horsepower.Value > eventItem.MaxHorsepower.Value)
            {
                return (false, $"Мощность автомобиля ({car.Horsepower} л.с.) превышает максимально допустимую ({eventItem.MaxHorsepower} л.с.)");
            }
        }

        if (!string.IsNullOrEmpty(eventItem.RequiredDriveType))
        {
            if (string.IsNullOrEmpty(car.DriveType) || car.DriveType != eventItem.RequiredDriveType)
            {
                return (false, $"Автомобиль не соответствует требуемому типу привода. Требуется: {eventItem.RequiredDriveType}");
            }
        }

        return (true, string.Empty);
    }

    private async Task<(bool IsValid, string ErrorMessage)> ValidateChampionshipRequirements(int participantId, Championship championship)
    {
        var participant = await _context.Participants
            .Include(p => p.Applications)
                .ThenInclude(a => a.FinalResult)
            .FirstOrDefaultAsync(p => p.Id == participantId);

        if (participant == null)
        {
            return (false, "Участник не найден");
        }

        var podiumsCount = participant.Applications
            .Count(a => a.FinalResult != null && a.FinalResult.Position <= 3);

        if (podiumsCount < championship.MinPodiumsRequired)
        {
            return (false, $"Для участия в чемпионате требуется минимум {championship.MinPodiumsRequired} подиумов. У вас: {podiumsCount}");
        }

        return (true, string.Empty);
    }
}

