using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceEvents.Data;
using RaceEvents.Models;
using RaceEvents.Models.Enums;
using RaceEvents.Models.ViewModels;

namespace RaceEvents.Controllers;

public class ResultsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ResultsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .Where(e => e.Status == EventStatus.COMPLETED || e.Status == EventStatus.IN_PROGRESS)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        return View(events);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
        {
            return NotFound();
        }

        var finalResults = await _context.FinalResults
            .Where(fr => fr.Application.EventId == id)
            .Include(fr => fr.Application)
            .ThenInclude(a => a.Participant)
            .Include(fr => fr.Application)
            .ThenInclude(a => a.Car)
            .OrderBy(fr => fr.Position)
            .ToListAsync();

        var results = finalResults.Select(fr => new ResultViewModel
        {
            Id = fr.Id,
            Position = fr.Position,
            ParticipantName = $"{fr.Application.Participant.LastName} {fr.Application.Participant.FirstName}",
            CarInfo = $"{fr.Application.Car.Brand} {fr.Application.Car.Model} ({fr.Application.Car.LicensePlate})",
            BestLapTime = FormatTimeSpan(fr.BestLapTime),
            AverageLapTime = FormatTimeSpan(fr.AverageLapTime),
            TotalTime = FormatTimeSpan(fr.TotalTime),
            TotalLaps = fr.TotalLaps
        }).ToList();

        var podium = results.Where(r => r.Position <= 3).OrderBy(r => r.Position).ToList();

        var viewModel = new EventResultsViewModel
        {
            EventId = eventEntity.Id,
            EventTitle = eventEntity.Title,
            EventDate = eventEntity.Date,
            EventLocation = eventEntity.Location,
            Results = results,
            Podium = podium
        };

        return View(viewModel);
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        var totalMilliseconds = (long)timeSpan.TotalMilliseconds;
        var milliseconds = totalMilliseconds % 1000;
        var tenths = milliseconds / 100;
        
        if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}.{tenths}";
        }
        
        var mins = timeSpan.Minutes;
        var secs = timeSpan.Seconds;
        return $"{mins:D2}:{secs:D2}.{tenths}";
    }
}

