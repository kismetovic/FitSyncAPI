using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Reservations;

public class ReservationCancelRequest
{
    [Required(ErrorMessage = "A cancellation reason is required.")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "The reason must be between 3 and 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}
