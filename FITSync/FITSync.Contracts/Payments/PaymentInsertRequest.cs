using System.ComponentModel.DataAnnotations;
using FITSync.Domain.Enums;

namespace FITSync.Contracts.Payments;

/// <summary>
/// Administrative payment entry (back-office correction). Regular clients never reach
/// this model - they go through the PayPal or cash flows, where the server sets the amount.
/// </summary>
public class PaymentInsertRequest
{
    [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Transaction ID is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Transaction ID cannot be empty.")]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code.")]
    public string Currency { get; set; } = "BAM";

    [EnumDataType(typeof(PaymentProvider), ErrorMessage = "Invalid payment provider.")]
    public PaymentProvider PaymentProvider { get; set; }

    [EnumDataType(typeof(PaymentStatus), ErrorMessage = "Invalid payment status.")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Captured;

    [Range(1, int.MaxValue, ErrorMessage = "A valid reservation must be selected.")]
    public int ReservationId { get; set; }
}
