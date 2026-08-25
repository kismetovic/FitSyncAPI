using System.ComponentModel.DataAnnotations;
using FITSync.Domain.Enums;

namespace FITSync.Contracts.Reservations;

/// <summary>
/// What a client is allowed to say when booking. Deliberately carries neither UserId nor
/// Status: the owner comes from the JWT and the initial status is decided by the server.
/// </summary>
public class ReservationInsertRequest
{
    [Required(ErrorMessage = "Reservation date is required.")]
    public DateTime ReservationDate { get; set; }

    [EnumDataType(typeof(ReservationType), ErrorMessage = "Invalid reservation type.")]
    public ReservationType ReservationType { get; set; } = ReservationType.OneTime;

    [Range(1, int.MaxValue, ErrorMessage = "A valid training must be selected.")]
    public int TrainingId { get; set; }

    public List<int> AdditionalServiceIds { get; set; } = new();

    /// <summary>
    /// Client asks for a slot outside the trainer's working hours. The server still verifies
    /// this against TrainerAvailability, applies the surcharge and forces PendingApproval;
    /// the flag alone grants nothing.
    /// </summary>
    public bool RequestOutsideAvailability { get; set; }

    /// <summary>
    /// Optional monthly package to draw this session from. Ignored unless
    /// ReservationType is Monthly and the membership genuinely belongs to the caller.
    /// </summary>
    public int? UserMembershipId { get; set; }
}
