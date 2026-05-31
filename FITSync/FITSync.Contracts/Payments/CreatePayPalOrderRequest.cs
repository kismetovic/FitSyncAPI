namespace FITSync.Contracts.Payments;

public class CreatePayPalOrderRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BAM";
    public int ReservationId { get; set; }
}
