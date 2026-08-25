using System.ComponentModel.DataAnnotations;
using FITSync.Domain.Enums;

namespace FITSync.Contracts.Reservations;

/// <summary>
/// Administrative edit of a reservation's schedule. Status is NOT part of this model -
/// status only ever moves through the dedicated approve / cancel / mark-paid / complete
/// actions so the state machine cannot be bypassed. Ownership is likewise not editable.
/// </summary>
public class ReservationUpdateRequest
{
    [Required(ErrorMessage = "Reservation date is required.")]
    public DateTime ReservationDate { get; set; }

    [EnumDataType(typeof(ReservationType), ErrorMessage = "Invalid reservation type.")]
    public ReservationType ReservationType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A valid training must be selected.")]
    public int TrainingId { get; set; }

    public List<int> AdditionalServiceIds { get; set; } = new();
}
