using FITSync.Contracts.Common;
using FITSync.Domain.Enums;

namespace FITSync.Contracts.Reservations;

public class ReservationSearchRequest : PagedRequest
{
    public int? UserId { get; set; }
    public int? TrainingId { get; set; }
    public ReservationStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Free-text match over the client's name, user name, e-mail or the training
    /// name. Applied in SQL so that searching does not force the whole table to
    /// be materialised, the same way UsersController.Search was fixed.
    /// </summary>
    public string? Query { get; set; }
}
