using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Payments;

/// <summary>Administrator confirming that cash was actually collected at the desk.</summary>
public class ConfirmCashPaymentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid reservation must be selected.")]
    public int ReservationId { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }
}
