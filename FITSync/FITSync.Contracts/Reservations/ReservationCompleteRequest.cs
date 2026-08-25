using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Reservations;

public class ReservationCompleteRequest
{
    [StringLength(500)]
    public string? Note { get; set; }
}
