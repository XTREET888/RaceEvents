using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;

namespace RaceEvents.Controllers;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (userRole == "ADMINISTRATOR")
        {
            var administrator = await _context.Administrators
                .Include(a => a.Events)
                    .ThenInclude(e => e.Championship)
                .Include(a => a.Events)
                    .ThenInclude(e => e.Applications)
                .FirstOrDefaultAsync(a => a.Id == userId);

            if (administrator == null)
            {
                return NotFound();
            }

            ViewBag.UserRole = "ADMINISTRATOR";
            ViewBag.EventsCount = administrator.Events.Count;
            ViewBag.ChampionshipsCount = await _context.Championships
                .Where(c => c.AdministratorId == userId)
                .CountAsync();

            return View("AdminProfile", administrator);
        }
        else
        {
            var participant = await _context.Participants
                .Include(p => p.Cars)
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Event)
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Car)
                .Include(p => p.Applications)
                    .ThenInclude(a => a.FinalResult)
                .FirstOrDefaultAsync(p => p.Id == userId);

            if (participant == null)
            {
                return NotFound();
            }

            var applicationsWithPodiums = participant.Applications
                .Where(a => a.FinalResult != null && a.FinalResult.Position <= 3)
                .OrderBy(a => a.Event.Date)
                .ToList();

            ViewBag.PodiumCount = applicationsWithPodiums.Count;
            ViewBag.BestLapTime = participant.BestLapTime;
            ViewBag.ApplicationsWithPodiums = applicationsWithPodiums;
            ViewBag.UserRole = "PARTICIPANT";

            return View(participant);
        }
    }
}




