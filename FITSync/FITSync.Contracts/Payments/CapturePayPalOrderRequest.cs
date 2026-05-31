namespace FITSync.Contracts.Payments;

public class CapturePayPalOrderRequest
{
    public string OrderId { get; set; } = string.Empty;
}
