using FITSync.Domain.Enums;

namespace FITSync.Contracts.Payments;

public class PaymentResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? ProviderOrderId { get; set; }
    public string Currency { get; set; } = "BAM";
    public PaymentProvider PaymentProvider { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? CapturedAt { get; set; }
    public string? FailureReason { get; set; }
    public int? ConfirmedByUserId { get; set; }
    /// <summary>Set when the payment settles a booking; null when it settles a package.</summary>
    public int? ReservationId { get; set; }

    /// <summary>Set when the payment settles a bought package; null for a booking.</summary>
    public int? UserMembershipId { get; set; }

    public string? UserName { get; set; }
    public string? UserEmail { get; set; }

    /// <summary>Name of the training a booking was for. Null for a package payment.</summary>
    public string? TrainingName { get; set; }

    /// <summary>Name of the package that was bought. Null for a booking payment.</summary>
    public string? MembershipPackageName { get; set; }
}
