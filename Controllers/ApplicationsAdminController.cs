using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.Enums;

namespace RaceEvents.Controllers;

public class ApplicationsAdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public ApplicationsAdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        IQueryable<Application> applicationsQuery = _context.Applications
            .Include(a => a.Event)
            .Include(a => a.Participant)
            .Include(a => a.Car)
            .OrderByDescending(a => a.ApplicationDate);

        if (!string.IsNullOrEmpty(status) && status != "ALL")
        {
            if (Enum.TryParse<ApplicationStatus>(status, out var statusEnum))
            {
                applicationsQuery = applicationsQuery.Where(a => a.Status == statusEnum);
            }
        }
        else if (string.IsNullOrEmpty(status))
        {
            applicationsQuery = applicationsQuery.Where(a => a.Status == ApplicationStatus.PENDING);
        }

        var applications = await applicationsQuery.ToListAsync();

        ViewBag.StatusFilter = status;
        ViewBag.PendingCount = await _context.Applications.CountAsync(a => a.Status == ApplicationStatus.PENDING);
        ViewBag.ApprovedCount = await _context.Applications.CountAsync(a => a.Status == ApplicationStatus.APPROVED);
        ViewBag.RejectedCount = await _context.Applications.CountAsync(a => a.Status == ApplicationStatus.REJECTED);

        return View(applications);
    }

    public async Task<IActionResult> Details(int? id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        if (id == null)
        {
            return NotFound();
        }

        var application = await _context.Applications
            .Include(a => a.Event)
                .ThenInclude(e => e.Championship)
            .Include(a => a.Participant)
            .Include(a => a.Car)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        ViewBag.CarValidation = ValidateCarForEvent(application.Car, application.Event);
        
        var approvedCount = await _context.Applications
            .CountAsync(a => a.EventId == application.EventId && a.Status == ApplicationStatus.APPROVED);
        ViewBag.ApprovedCount = approvedCount;
        ViewBag.MaxParticipants = application.Event.MaxParticipants;
        ViewBag.CanApprove = approvedCount < application.Event.MaxParticipants;

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var application = await _context.Applications
            .Include(a => a.Event)
            .Include(a => a.Car)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        var validation = ValidateCarForEvent(application.Car, application.Event);
        if (!validation.IsValid)
        {
            TempData["ErrorMessage"] = validation.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        var approvedCount = await _context.Applications
            .CountAsync(a => a.EventId == application.EventId && a.Status == ApplicationStatus.APPROVED);

        if (approvedCount >= application.Event.MaxParticipants)
        {
            TempData["ErrorMessage"] = "Достигнуто максимальное количество участников для этого события";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (application.Event.ChampionshipId.HasValue)
        {
            var championship = await _context.Championships
                .FirstOrDefaultAsync(c => c.Id == application.Event.ChampionshipId.Value);

            if (championship != null)
            {
                var championshipValidation = await ValidateChampionshipRequirements(application.ParticipantId, championship);
                if (!championshipValidation.IsValid)
                {
                    TempData["ErrorMessage"] = championshipValidation.ErrorMessage;
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
        }

        application.Status = ApplicationStatus.APPROVED;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Заявка одобрена";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var application = await _context.Applications.FindAsync(id);

        if (application == null)
        {
            return NotFound();
        }

        if (application.Status != ApplicationStatus.PENDING)
        {
            TempData["ErrorMessage"] = "Можно отклонить только заявки со статусом 'Ожидает рассмотрения'";
            return RedirectToAction(nameof(Details), new { id });
        }

        application.Status = ApplicationStatus.REJECTED;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Заявка отклонена";
        return RedirectToAction(nameof(Index));
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
            return (false, $"Для участия в чемпионате требуется минимум {championship.MinPodiumsRequired} подиумов. У участника: {podiumsCount}");
        }

        return (true, string.Empty);
    }
}

