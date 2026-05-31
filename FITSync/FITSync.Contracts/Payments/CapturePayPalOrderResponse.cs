namespace FITSync.Contracts.Payments;

public class CapturePayPalOrderResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
