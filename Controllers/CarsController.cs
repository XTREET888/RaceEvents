using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.ViewModels;

namespace RaceEvents.Controllers;

public class CarsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CarsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var cars = await _context.Cars
            .Where(c => c.ParticipantId == userId)
            .ToListAsync();

        return View(cars);
    }

    public IActionResult Create()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            var existingCar = await _context.Cars
                .FirstOrDefaultAsync(c => c.LicensePlate == model.LicensePlate);
            
            if (existingCar != null)
            {
                ModelState.AddModelError("LicensePlate", "Автомобиль с таким госномером уже существует");
                return View(model);
            }

            var car = new Car
            {
                Brand = model.Brand,
                Model = model.Model,
                CarClass = model.CarClass,
                Year = model.Year,
                Color = model.Color,
                LicensePlate = model.LicensePlate,
                Horsepower = model.Horsepower,
                DriveType = model.DriveType,
                ParticipantId = userId.Value
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .FirstOrDefaultAsync(c => c.Id == id && c.ParticipantId == userId);

        if (car == null)
        {
            return NotFound();
        }

        var model = new CarViewModel
        {
            Id = car.Id,
            Brand = car.Brand,
            Model = car.Model,
            CarClass = car.CarClass,
            Year = car.Year,
            Color = car.Color,
            LicensePlate = car.LicensePlate,
            Horsepower = car.Horsepower,
            DriveType = car.DriveType
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CarViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.Id == id && c.ParticipantId == userId);

            if (car == null)
            {
                return NotFound();
            }

            var existingCar = await _context.Cars
                .FirstOrDefaultAsync(c => c.LicensePlate == model.LicensePlate && c.Id != id);
            
            if (existingCar != null)
            {
                ModelState.AddModelError("LicensePlate", "Автомобиль с таким госномером уже существует");
                return View(model);
            }

            car.Brand = model.Brand;
            car.Model = model.Model;
            car.CarClass = model.CarClass;
            car.Year = model.Year;
            car.Color = model.Color;
            car.LicensePlate = model.LicensePlate;
            car.Horsepower = model.Horsepower;
            car.DriveType = model.DriveType;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarExists(car.Id))
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
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .FirstOrDefaultAsync(c => c.Id == id && c.ParticipantId == userId);

        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var car = await _context.Cars
            .FirstOrDefaultAsync(c => c.Id == id && c.ParticipantId == userId);

        if (car != null)
        {
            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool CarExists(int id)
    {
        return _context.Cars.Any(e => e.Id == id);
    }
}

