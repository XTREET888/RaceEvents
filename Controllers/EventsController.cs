using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.Enums;
using RaceEvents.Models.ViewModels;

namespace RaceEvents.Controllers;

public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .Include(e => e.Administrator)
            .Include(e => e.Applications)
            .OrderBy(e => e.Date)
            .ToListAsync();

        return View(events);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events
            .Include(e => e.Administrator)
            .Include(e => e.Championship)
            .Include(e => e.Applications)
                .ThenInclude(a => a.Participant)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        return View(eventItem);
    }

    public IActionResult Create()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.Championships = _context.Championships.ToList();
        
        ViewBag.EventTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "WINTER", Text = "ЗИМНИЙ" },
            new SelectListItem { Value = "SUMMER", Text = "ЛЕТНИЙ" },
            new SelectListItem { Value = "TRACK_DAY", Text = "ТРЕК ДЕНЬ" }
        };
        
        ViewBag.CarTypeRequirements = new List<SelectListItem>
        {
            new SelectListItem { Value = "ANY", Text = "ЛЮБОЙ" },
            new SelectListItem { Value = "SPECIFIC_CLASS", Text = "ОПРЕДЕЛЕННЫЙ КЛАСС" }
        };
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            var eventItem = new Event
            {
                Title = model.Title,
                Description = model.Description,
                Date = model.Date,
                Location = model.Location,
                TrackType = model.TrackType,
                MaxParticipants = model.MaxParticipants,
                Status = EventStatus.UPCOMING,
                Type = model.Type,
                CarTypeRequirement = model.CarTypeRequirement,
                RequiredCarClass = model.RequiredCarClass,
                MaxHorsepower = model.MaxHorsepower,
                RequiredDriveType = model.RequiredDriveType,
                AdministratorId = userId.Value,
                ChampionshipId = model.ChampionshipId
            };

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Championships = _context.Championships.ToList();
        
        ViewBag.EventTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "WINTER", Text = "ЗИМНИЙ" },
            new SelectListItem { Value = "SUMMER", Text = "ЛЕТНИЙ" },
            new SelectListItem { Value = "TRACK_DAY", Text = "ТРЕК ДЕНЬ" }
        };
        
        ViewBag.CarTypeRequirements = new List<SelectListItem>
        {
            new SelectListItem { Value = "ANY", Text = "ЛЮБОЙ" },
            new SelectListItem { Value = "SPECIFIC_CLASS", Text = "ОПРЕДЕЛЕННЫЙ КЛАСС" }
        };
        
        return View(model);
    }

    public async Task<IActionResult> Edit(int? id)
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

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.AdministratorId == userId);

        if (eventItem == null)
        {
            return NotFound();
        }

        var model = new EventViewModel
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            Date = eventItem.Date,
            Location = eventItem.Location,
            TrackType = eventItem.TrackType,
            MaxParticipants = eventItem.MaxParticipants,
            Type = eventItem.Type,
            CarTypeRequirement = eventItem.CarTypeRequirement,
            RequiredCarClass = eventItem.RequiredCarClass,
            MaxHorsepower = eventItem.MaxHorsepower,
            RequiredDriveType = eventItem.RequiredDriveType,
            ChampionshipId = eventItem.ChampionshipId
        };

        ViewBag.Championships = _context.Championships.ToList();
        
        ViewBag.EventTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "WINTER", Text = "ЗИМНИЙ" },
            new SelectListItem { Value = "SUMMER", Text = "ЛЕТНИЙ" },
            new SelectListItem { Value = "TRACK_DAY", Text = "ТРЕК ДЕНЬ" }
        };
        
        ViewBag.CarTypeRequirements = new List<SelectListItem>
        {
            new SelectListItem { Value = "ANY", Text = "ЛЮБОЙ" },
            new SelectListItem { Value = "SPECIFIC_CLASS", Text = "ОПРЕДЕЛЕННЫЙ КЛАСС" }
        };
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.AdministratorId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            eventItem.Title = model.Title;
            eventItem.Description = model.Description;
            eventItem.Date = model.Date;
            eventItem.Location = model.Location;
            eventItem.TrackType = model.TrackType;
            eventItem.MaxParticipants = model.MaxParticipants;
            eventItem.Type = model.Type;
            eventItem.CarTypeRequirement = model.CarTypeRequirement;
            eventItem.RequiredCarClass = model.RequiredCarClass;
            eventItem.MaxHorsepower = model.MaxHorsepower;
            eventItem.RequiredDriveType = model.RequiredDriveType;
            eventItem.ChampionshipId = model.ChampionshipId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(eventItem.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Championships = _context.Championships.ToList();
        
        ViewBag.EventTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "WINTER", Text = "ЗИМНИЙ" },
            new SelectListItem { Value = "SUMMER", Text = "ЛЕТНИЙ" },
            new SelectListItem { Value = "TRACK_DAY", Text = "ТРЕК ДЕНЬ" }
        };
        
        ViewBag.CarTypeRequirements = new List<SelectListItem>
        {
            new SelectListItem { Value = "ANY", Text = "ЛЮБОЙ" },
            new SelectListItem { Value = "SPECIFIC_CLASS", Text = "ОПРЕДЕЛЕННЫЙ КЛАСС" }
        };
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, EventStatus status)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.AdministratorId == userId);

        if (eventItem != null)
        {
            eventItem.Status = status;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool EventExists(int id)
    {
        return _context.Events.Any(e => e.Id == id);
    }
}




