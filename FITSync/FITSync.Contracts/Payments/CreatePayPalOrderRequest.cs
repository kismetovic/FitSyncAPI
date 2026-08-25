using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Payments;

/// <summary>
/// The client only names the reservation it wants to pay. Amount and currency are read
/// from the reservation on the server; they are not accepted from the client.
/// </summary>
public class CreatePayPalOrderRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid reservation must be selected.")]
    public int ReservationId { get; set; }
}
