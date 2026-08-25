using FITSync.Domain.Enums;

namespace FITSync.Contracts.Payments;

public class CapturePayPalOrderResponse
{
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Raw PayPal capture status, e.g. COMPLETED.</summary>
    public string Status { get; set; } = string.Empty;

    public PaymentStatus PaymentStatus { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BAM";
    public int? ReservationId { get; set; }

    /// <summary>Reservation status after the capture was verified and persisted.</summary>
    public ReservationStatus? ReservationStatus { get; set; }

    public int? UserMembershipId { get; set; }

    /// <summary>Package status after the capture. Active once the money is in.</summary>
    public MembershipStatus? MembershipStatus { get; set; }

    public PaymentResponse? Payment { get; set; }
}
