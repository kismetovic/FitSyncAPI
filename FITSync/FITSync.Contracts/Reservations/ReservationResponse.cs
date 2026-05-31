using FITSync.Domain.Enums;

namespace FITSync.Contracts.Reservations;

public class ReservationResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ReservationDate { get; set; }
    public ReservationStatus Status { get; set; }
    public ReservationType ReservationType { get; set; }
    public int UserId { get; set; }
    public int TrainingId { get; set; }
    public UserSummaryResponse? User { get; set; }
    public TrainingSummaryResponse? Training { get; set; }
    public List<int> AdditionalServiceIds { get; set; } = new();
}

public class UserSummaryResponse
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
}

public class TrainingSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}
