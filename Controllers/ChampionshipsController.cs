using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.ViewModels;

namespace RaceEvents.Controllers;

public class ChampionshipsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ChampionshipsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        var isAdmin = userRole == "ADMINISTRATOR";

        var championships = await _context.Championships
            .Include(c => c.Administrator)
            .Include(c => c.Events)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        ViewBag.IsAdmin = isAdmin;
        ViewBag.UserId = userId;

        return View(championships);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        var isAdmin = userRole == "ADMINISTRATOR";

        var championship = await _context.Championships
            .Include(c => c.Administrator)
            .Include(c => c.Events)
                .ThenInclude(e => e.Applications)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (championship == null)
        {
            return NotFound();
        }

        ViewBag.IsAdmin = isAdmin;
        ViewBag.UserId = userId;

        return View(championship);
    }

    public IActionResult Create()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChampionshipViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Дата окончания должна быть позже даты начала");
                return View(model);
            }

            var championship = new Championship
            {
                Title = model.Title,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                RequiredCarClass = model.RequiredCarClass,
                MinPodiumsRequired = model.MinPodiumsRequired,
                AdministratorId = userId.Value
            };

            _context.Championships.Add(championship);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

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

        var championship = await _context.Championships
            .FirstOrDefaultAsync(c => c.Id == id && c.AdministratorId == userId);

        if (championship == null)
        {
            return NotFound();
        }

        var model = new ChampionshipViewModel
        {
            Id = championship.Id,
            Title = championship.Title,
            Description = championship.Description,
            StartDate = championship.StartDate,
            EndDate = championship.EndDate,
            RequiredCarClass = championship.RequiredCarClass,
            MinPodiumsRequired = championship.MinPodiumsRequired
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ChampionshipViewModel model)
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
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Дата окончания должна быть позже даты начала");
                return View(model);
            }

            var championship = await _context.Championships
                .FirstOrDefaultAsync(c => c.Id == id && c.AdministratorId == userId);

            if (championship == null)
            {
                return NotFound();
            }

            championship.Title = model.Title;
            championship.Description = model.Description;
            championship.StartDate = model.StartDate;
            championship.EndDate = model.EndDate;
            championship.RequiredCarClass = model.RequiredCarClass;
            championship.MinPodiumsRequired = model.MinPodiumsRequired;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChampionshipExists(championship.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    public async Task<IActionResult> Delete(int? id)
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

        var championship = await _context.Championships
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == id && c.AdministratorId == userId);

        if (championship == null)
        {
            return NotFound();
        }

        return View(championship);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var championship = await _context.Championships
            .FirstOrDefaultAsync(c => c.Id == id && c.AdministratorId == userId);

        if (championship != null)
        {
            _context.Championships.Remove(championship);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ChampionshipExists(int id)
    {
        return _context.Championships.Any(e => e.Id == id);
    }
}

