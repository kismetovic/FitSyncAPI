namespace FITSync.Contracts.Trainers;

public class TrainerResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string? Specialty { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public decimal OutsideAvailabilitySurcharge { get; set; }
    public int? UserId { get; set; }
    public List<TrainerAvailabilityResponse> Availabilities { get; set; } = new();
}

public class TrainerAvailabilityResponse
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
