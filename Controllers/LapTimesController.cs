using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.Enums;
using RaceEvents.Models.ViewModels;

namespace RaceEvents.Controllers;

public class LapTimesController : Controller
{
    private readonly ApplicationDbContext _context;

    public LapTimesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var events = await _context.Events
            .Where(e => e.Status == EventStatus.IN_PROGRESS)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        ViewBag.Events = new SelectList(events, "Id", "Title", eventId);

        if (eventId.HasValue)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId.Value);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var applications = await _context.Applications
                .Where(a => a.EventId == eventId.Value && a.Status == ApplicationStatus.APPROVED)
                .Include(a => a.Participant)
                .Include(a => a.Car)
                .Include(a => a.LapTimes.OrderBy(lt => lt.LapNumber))
                .OrderBy(a => a.Participant.LastName)
                .ThenBy(a => a.Participant.FirstName)
                .ToListAsync();

            ViewBag.Event = eventEntity;
            ViewBag.Applications = applications;
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? eventId, int? applicationId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var events = await _context.Events
            .Where(e => e.Status == EventStatus.IN_PROGRESS)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        ViewBag.Events = new SelectList(events, "Id", "Title", eventId);

        if (eventId.HasValue)
        {
            var applications = await _context.Applications
                .Where(a => a.EventId == eventId.Value && a.Status == ApplicationStatus.APPROVED)
                .Include(a => a.Participant)
                .Include(a => a.Car)
                .OrderBy(a => a.Participant.LastName)
                .ThenBy(a => a.Participant.FirstName)
                .ToListAsync();

            ViewBag.Applications = new SelectList(
                applications.Select(a => new
                {
                    Id = a.Id,
                    Name = $"{a.Participant.LastName} {a.Participant.FirstName} - {a.Car.Brand} {a.Car.Model} ({a.Car.LicensePlate})"
                }),
                "Id",
                "Name",
                applicationId);

            if (applicationId.HasValue)
            {
                var application = await _context.Applications
                    .Include(a => a.LapTimes)
                    .FirstOrDefaultAsync(a => a.Id == applicationId.Value);

                if (application != null)
                {
                    var maxLapNumber = application.LapTimes.Any() 
                        ? application.LapTimes.Max(lt => lt.LapNumber) 
                        : 0;

                    var model = new LapTimeViewModel
                    {
                        EventId = eventId.Value,
                        ApplicationId = applicationId.Value,
                        LapNumber = maxLapNumber + 1
                    };

                    return View(model);
                }
            }
        }

        return View(new LapTimeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LapTimeViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            var application = await _context.Applications
                .Include(a => a.Event)
                .FirstOrDefaultAsync(a => a.Id == model.ApplicationId);

            if (application == null)
            {
                ModelState.AddModelError("", "Заявка не найдена");
            }
            else if (application.EventId != model.EventId)
            {
                ModelState.AddModelError("", "Заявка не относится к выбранному событию");
            }
            else if (application.Status != ApplicationStatus.APPROVED)
            {
                ModelState.AddModelError("", "Можно записывать время только для одобренных заявок");
            }
            else
            {
                if (!TryParseTime(model.Time, out var timeSpan))
                {
                    ModelState.AddModelError("Time", "Некорректный формат времени. Используйте формат ЧЧ:ММ:СС (например, 01:23:45) или ММ:СС.С (например, 01:23.4)");
                }
                else
                {
                    var existingLap = await _context.LapTimes
                        .FirstOrDefaultAsync(lt => lt.ApplicationId == model.ApplicationId && lt.LapNumber == model.LapNumber);

                    if (existingLap != null)
                    {
                        ModelState.AddModelError("LapNumber", $"Время для круга {model.LapNumber} уже записано");
                    }
                    else
                    {
                        var lapTime = new LapTime
                        {
                            ApplicationId = model.ApplicationId,
                            LapNumber = model.LapNumber,
                            Time = timeSpan,
                            RecordedAt = DateTime.Now
                        };

                        _context.LapTimes.Add(lapTime);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Время круга {model.LapNumber} успешно записано";
                        return RedirectToAction("Index", new { eventId = model.EventId });
                    }
                }
            }
        }

        var events = await _context.Events
            .Where(e => e.Status == EventStatus.IN_PROGRESS)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        ViewBag.Events = new SelectList(events, "Id", "Title", model.EventId);

        if (model.EventId > 0)
        {
            var applications = await _context.Applications
                .Where(a => a.EventId == model.EventId && a.Status == ApplicationStatus.APPROVED)
                .Include(a => a.Participant)
                .Include(a => a.Car)
                .OrderBy(a => a.Participant.LastName)
                .ThenBy(a => a.Participant.FirstName)
                .ToListAsync();

            ViewBag.Applications = new SelectList(
                applications.Select(a => new
                {
                    Id = a.Id,
                    Name = $"{a.Participant.LastName} {a.Participant.FirstName} - {a.Car.Brand} {a.Car.Model} ({a.Car.LicensePlate})"
                }),
                "Id",
                "Name",
                model.ApplicationId);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CalculateResults(int eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var applications = await _context.Applications
            .Where(a => a.EventId == eventId && a.Status == ApplicationStatus.APPROVED)
            .Include(a => a.LapTimes)
            .Include(a => a.Participant)
            .ToListAsync();

        var results = new List<(Application application, TimeSpan bestLap, TimeSpan totalTime, TimeSpan averageLap, int totalLaps)>();

        foreach (var application in applications)
        {
            if (!application.LapTimes.Any())
            {
                continue;
            }

            var lapTimes = application.LapTimes.OrderBy(lt => lt.LapNumber).ToList();
            var bestLap = lapTimes.Min(lt => lt.Time);
            var totalTime = lapTimes.Aggregate(TimeSpan.Zero, (sum, lt) => sum + lt.Time);
            var totalLaps = lapTimes.Count;
            var averageLap = TimeSpan.FromTicks(totalTime.Ticks / totalLaps);

            results.Add((application, bestLap, totalTime, averageLap, totalLaps));
        }

        var sortedResults = results.OrderBy(r => r.totalTime).ThenBy(r => r.bestLap).ToList();

        for (int i = 0; i < sortedResults.Count; i++)
        {
            var (application, bestLap, totalTime, averageLap, totalLaps) = sortedResults[i];
            var position = i + 1;

            var existingResult = await _context.FinalResults
                .FirstOrDefaultAsync(fr => fr.ApplicationId == application.Id);

            var wasFirstCalculation = existingResult == null;

            if (existingResult != null)
            {
                existingResult.BestLapTime = bestLap;
                existingResult.TotalTime = totalTime;
                existingResult.AverageLapTime = averageLap;
                existingResult.TotalLaps = totalLaps;
                existingResult.Position = position;
            }
            else
            {
                var finalResult = new FinalResult
                {
                    ApplicationId = application.Id,
                    BestLapTime = bestLap,
                    TotalTime = totalTime,
                    AverageLapTime = averageLap,
                    TotalLaps = totalLaps,
                    Position = position
                };

                _context.FinalResults.Add(finalResult);
            }

            if (position <= 3 && wasFirstCalculation)
            {
                var participant = await _context.Participants.FindAsync(application.ParticipantId);
                if (participant != null)
                {
                    participant.PodiumCount += 1;
                }
            }

            if (application.Participant.BestLapTime == null || bestLap < application.Participant.BestLapTime)
            {
                var participant = await _context.Participants.FindAsync(application.ParticipantId);
                if (participant != null)
                {
                    participant.BestLapTime = bestLap;
                }
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Результаты успешно рассчитаны и обновлены";
        return RedirectToAction("Index", new { eventId = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> SetManualPositions(int eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            return NotFound();
        }

        var applications = await _context.Applications
            .Where(a => a.EventId == eventId && a.Status == ApplicationStatus.APPROVED)
            .Include(a => a.Participant)
            .Include(a => a.Car)
            .Include(a => a.FinalResult)
            .OrderBy(a => a.Participant.LastName)
            .ThenBy(a => a.Participant.FirstName)
            .ToListAsync();

        var positions = applications.Select(a => new ManualPositionViewModel
        {
            ApplicationId = a.Id,
            ParticipantName = $"{a.Participant.LastName} {a.Participant.FirstName}",
            CarInfo = $"{a.Car.Brand} {a.Car.Model} ({a.Car.LicensePlate})",
            Position = a.FinalResult?.Position ?? 0
        }).ToList();

        var viewModel = new EventManualPositionsViewModel
        {
            EventId = eventId,
            EventTitle = eventEntity.Title,
            Positions = positions
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetManualPositions(EventManualPositionsViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (userId == null || userRole != "ADMINISTRATOR")
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            var positions = model.Positions.Where(p => p.Position > 0).OrderBy(p => p.Position).ToList();
            
            var duplicatePositions = positions
                .GroupBy(p => p.Position)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatePositions.Any())
            {
                ModelState.AddModelError("", $"Обнаружены дублирующиеся места: {string.Join(", ", duplicatePositions)}");
            }
            else
            {
                foreach (var positionModel in model.Positions)
                {
                    if (positionModel.Position <= 0)
                    {
                        continue;
                    }

                    var application = await _context.Applications
                        .Include(a => a.LapTimes)
                        .Include(a => a.Participant)
                        .FirstOrDefaultAsync(a => a.Id == positionModel.ApplicationId);

                    if (application == null)
                    {
                        continue;
                    }

                    var existingResult = await _context.FinalResults
                        .FirstOrDefaultAsync(fr => fr.ApplicationId == application.Id);

                    var wasFirstCalculation = existingResult == null;

                    var bestLap = application.LapTimes.Any() 
                        ? application.LapTimes.Min(lt => lt.Time) 
                        : TimeSpan.Zero;
                    var totalTime = application.LapTimes.Any()
                        ? application.LapTimes.Aggregate(TimeSpan.Zero, (sum, lt) => sum + lt.Time)
                        : TimeSpan.Zero;
                    var totalLaps = application.LapTimes.Count;
                    var averageLap = totalLaps > 0 
                        ? TimeSpan.FromTicks(totalTime.Ticks / totalLaps) 
                        : TimeSpan.Zero;

                    if (existingResult != null)
                    {
                        existingResult.BestLapTime = bestLap;
                        existingResult.TotalTime = totalTime;
                        existingResult.AverageLapTime = averageLap;
                        existingResult.TotalLaps = totalLaps;
                        existingResult.Position = positionModel.Position;
                    }
                    else
                    {
                        var finalResult = new FinalResult
                        {
                            ApplicationId = application.Id,
                            BestLapTime = bestLap,
                            TotalTime = totalTime,
                            AverageLapTime = averageLap,
                            TotalLaps = totalLaps,
                            Position = positionModel.Position
                        };

                        _context.FinalResults.Add(finalResult);
                    }

                    if (positionModel.Position <= 3 && wasFirstCalculation)
                    {
                        var participant = await _context.Participants.FindAsync(application.ParticipantId);
                        if (participant != null)
                        {
                            participant.PodiumCount += 1;
                        }
                    }

                    if (application.LapTimes.Any() && (application.Participant.BestLapTime == null || bestLap < application.Participant.BestLapTime))
                    {
                        var participant = await _context.Participants.FindAsync(application.ParticipantId);
                        if (participant != null)
                        {
                            participant.BestLapTime = bestLap;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Места участников успешно сохранены";
                return RedirectToAction("Index", new { eventId = model.EventId });
            }
        }

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == model.EventId);

        if (eventEntity != null)
        {
            model.EventTitle = eventEntity.Title;
        }

        return View(model);
    }

    private bool TryParseTime(string timeString, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return false;
        }

        timeString = timeString.Trim();

        var parts = timeString.Split(':');
        
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out var minutes))
            {
                var secondsPart = parts[1].Split('.');
                if (secondsPart.Length >= 1 && int.TryParse(secondsPart[0], out var seconds))
                {
                    var milliseconds = 0;
                    if (secondsPart.Length > 1)
                    {
                        var msString = secondsPart[1];
                        if (msString.Length == 1)
                        {
                            if (int.TryParse(msString, out var tenths))
                            {
                                milliseconds = tenths * 100;
                            }
                        }
                        else
                        {
                            msString = msString.PadRight(3, '0').Substring(0, Math.Min(3, msString.Length));
                            int.TryParse(msString, out milliseconds);
                        }
                    }
                    
                    timeSpan = new TimeSpan(0, 0, minutes, seconds, milliseconds);
                    return true;
                }
            }
        }
        else if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out var hours) &&
                int.TryParse(parts[1], out var minutes))
            {
                var secondsPart = parts[2].Split('.');
                if (secondsPart.Length >= 1 && int.TryParse(secondsPart[0], out var seconds))
                {
                    var milliseconds = 0;
                    if (secondsPart.Length > 1)
                    {
                        var msString = secondsPart[1];
                        if (msString.Length == 1)
                        {
                            if (int.TryParse(msString, out var tenths))
                            {
                                milliseconds = tenths * 100;
                            }
                        }
                        else
                        {
                            msString = msString.PadRight(3, '0').Substring(0, Math.Min(3, msString.Length));
                            int.TryParse(msString, out milliseconds);
                        }
                    }
                    
                    timeSpan = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                    return true;
                }
            }
        }
        else if (TimeSpan.TryParse(timeString, out timeSpan))
        {
            return true;
        }

        return false;
    }
}

