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
        var userRole = HttpContext.Session.GetString("UserRole");
        
        if (userId == null || userRole != "PARTICIPANT")
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
        var userRole = HttpContext.Session.GetString("UserRole");
        
        if (userId == null || userRole != "PARTICIPANT")
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarViewModel carViewModel)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        
        if (userId == null || userRole != "PARTICIPANT")
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.Id == userId.Value);
            
            if (participant == null)
            {
                ModelState.AddModelError("", "Пользователь не найден");
                return View(carViewModel);
            }

            var existingCar = await _context.Cars
                .FirstOrDefaultAsync(c => c.LicensePlate == carViewModel.LicensePlate);
            
            if (existingCar != null)
            {
                ModelState.AddModelError("LicensePlate", "Автомобиль с таким госномером уже существует");
                return View(carViewModel);
            }

            if (!carViewModel.Year.HasValue || carViewModel.Year.Value < 1900 || carViewModel.Year.Value > 2100)
            {
                ModelState.AddModelError("Year", "Год обязателен и должен быть от 1900 до 2100");
                return View(carViewModel);
            }

            var car = new Car
            {
                Brand = carViewModel.Brand,
                Model = carViewModel.Model,
                CarClass = carViewModel.CarClass,
                Year = carViewModel.Year!.Value,
                Color = carViewModel.Color,
                LicensePlate = carViewModel.LicensePlate,
                Horsepower = carViewModel.Horsepower,
                DriveType = carViewModel.DriveType,
                ParticipantId = userId.Value
            };

            try
            {
                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
                return View(carViewModel);
            }
        }

        return View(carViewModel);
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
    public async Task<IActionResult> Edit(int id, CarViewModel carViewModel)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != carViewModel.Id)
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
                .FirstOrDefaultAsync(c => c.LicensePlate == carViewModel.LicensePlate && c.Id != id);
            
            if (existingCar != null)
            {
                ModelState.AddModelError("LicensePlate", "Автомобиль с таким госномером уже существует");
                return View(carViewModel);
            }

            car.Brand = carViewModel.Brand;
            car.Model = carViewModel.Model;
            car.CarClass = carViewModel.CarClass;
            car.Year = carViewModel.Year ?? 0;
            car.Color = carViewModel.Color;
            car.LicensePlate = carViewModel.LicensePlate;
            car.Horsepower = carViewModel.Horsepower;
            car.DriveType = carViewModel.DriveType;

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

        return View(carViewModel);
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




