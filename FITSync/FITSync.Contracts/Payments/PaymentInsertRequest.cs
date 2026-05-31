using FITSync.Domain.Enums;

namespace FITSync.Contracts.Payments;

public class PaymentInsertRequest
{
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Currency { get; set; } = "BAM";
    public PaymentProvider PaymentProvider { get; set; }
    public int ReservationId { get; set; }
}
