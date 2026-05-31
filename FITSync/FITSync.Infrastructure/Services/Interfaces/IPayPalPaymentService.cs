namespace FITSync.Infrastructure.Services.Interfaces;

public interface IPayPalPaymentService
{
    Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string currency, int reservationId, CancellationToken cancellationToken = default);

    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default);
}

public class PayPalOrderResult
{
    public string OrderId { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
}

public class PayPalCaptureResult
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
