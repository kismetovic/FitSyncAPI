using FITSync.Domain.Enums;

namespace FITSync.Contracts.Payments;

public class PaymentResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Currency { get; set; } = "BAM";
    public PaymentProvider PaymentProvider { get; set; }
    public int ReservationId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? TrainingName { get; set; }
}
