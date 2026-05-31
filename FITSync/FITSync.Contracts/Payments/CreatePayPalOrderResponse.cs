namespace FITSync.Contracts.Payments;

public class CreatePayPalOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
}
