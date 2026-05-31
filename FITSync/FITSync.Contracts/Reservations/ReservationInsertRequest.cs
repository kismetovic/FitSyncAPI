using FITSync.Domain.Enums;

namespace FITSync.Contracts.Reservations;

public class ReservationInsertRequest
{
    public DateTime ReservationDate { get; set; }
    public ReservationStatus Status { get; set; }
    public ReservationType ReservationType { get; set; } = ReservationType.OneTime;
    public int UserId { get; set; }
    public int TrainingId { get; set; }
    public List<int> AdditionalServiceIds { get; set; } = new();
}
