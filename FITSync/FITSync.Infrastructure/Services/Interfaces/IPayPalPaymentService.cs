namespace FITSync.Infrastructure.Services.Interfaces;

public interface IPayPalPaymentService
{
    Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string currency, string reference, CancellationToken cancellationToken = default);

    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default);
}

public class PayPalOrderResult
{
    public string OrderId { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
}

/// <summary>
/// Everything the backend needs to verify a capture before trusting it: not just the
/// transaction id, but the amount, currency and reference actually charged by PayPal.
/// </summary>
public class PayPalCaptureResult
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;

    /// <summary>PayPal reference_id: "reservation-{id}" or "membership-{id}".</summary>
    public string ReferenceId { get; set; } = string.Empty;

    public bool IsCompleted => string.Equals(Status, "COMPLETED", StringComparison.OrdinalIgnoreCase);
}
