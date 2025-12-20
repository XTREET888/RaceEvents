using System.ComponentModel.DataAnnotations;

namespace RaceEvents.Models.ViewModels;

public class ResultViewModel
{
    public int Id { get; set; }
    public int Position { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CarInfo { get; set; } = string.Empty;
    public string BestLapTime { get; set; } = string.Empty;
    public string AverageLapTime { get; set; } = string.Empty;
    public string TotalTime { get; set; } = string.Empty;
    public int TotalLaps { get; set; }
}

public class EventResultsViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventLocation { get; set; } = string.Empty;
    public List<ResultViewModel> Results { get; set; } = new List<ResultViewModel>();
    public List<ResultViewModel> Podium { get; set; } = new List<ResultViewModel>();
}

