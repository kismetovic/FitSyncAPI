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

    /// <summary>Server-calculated price. The payment flow charges exactly this.</summary>
    public decimal TotalPrice { get; set; }

    public bool IsOutsideTrainerAvailability { get; set; }
    public decimal OutsideAvailabilitySurcharge { get; set; }

    public int? UserMembershipId { get; set; }

    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Statuses this reservation may legally move to next.</summary>
    public List<ReservationStatus> AllowedNextStatuses { get; set; } = new();

    /// <summary>True once a captured payment exists for this reservation.</summary>
    public bool IsPaid { get; set; }

    public UserSummaryResponse? User { get; set; }
    public TrainingSummaryResponse? Training { get; set; }
    public List<int> AdditionalServiceIds { get; set; } = new();
    public List<ReservationStatusHistoryResponse> StatusHistory { get; set; } = new();
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
    public int MaxCapacity { get; set; }
    public int? TrainerId { get; set; }
    public string? TrainerName { get; set; }
}

public class ReservationStatusHistoryResponse
{
    public int Id { get; set; }
    public ReservationStatus FromStatus { get; set; }
    public ReservationStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? Reason { get; set; }
}
