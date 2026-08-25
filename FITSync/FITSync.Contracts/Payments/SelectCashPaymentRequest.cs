using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Payments;

/// <summary>
/// A client choosing "pay on arrival". This only records the intent - it does not mark
/// the reservation as paid. Only an administrator can confirm that cash was received.
/// </summary>
public class SelectCashPaymentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid reservation must be selected.")]
    public int ReservationId { get; set; }
}
