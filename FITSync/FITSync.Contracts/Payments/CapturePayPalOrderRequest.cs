using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Payments;

public class CapturePayPalOrderRequest
{
    [Required(ErrorMessage = "PayPal order id is required.")]
    [StringLength(100, MinimumLength = 1)]
    public string OrderId { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A valid reservation must be selected.")]
    public int ReservationId { get; set; }
}
