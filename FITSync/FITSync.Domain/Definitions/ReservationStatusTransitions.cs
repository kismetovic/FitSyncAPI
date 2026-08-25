using FITSync.Domain.Enums;

namespace FITSync.Domain.Definitions;

/// <summary>
/// Single source of truth for the reservation state machine. Every status change in
/// the system goes through <see cref="CanTransition"/>; there is no generic "set the
/// status to whatever the client sent" path anywhere in the API.
/// </summary>
public static class ReservationStatusTransitions
{
    private static readonly IReadOnlyDictionary<ReservationStatus, ReservationStatus[]> Allowed =
        new Dictionary<ReservationStatus, ReservationStatus[]>
        {
            [ReservationStatus.Initial] = new[]
            {
                ReservationStatus.Approved,
                ReservationStatus.Paid,
                ReservationStatus.Cancelled
            },
            [ReservationStatus.PendingApproval] = new[]
            {
                ReservationStatus.Approved,
                ReservationStatus.Cancelled
            },
            [ReservationStatus.Approved] = new[]
            {
                ReservationStatus.Paid,
                ReservationStatus.Cancelled
            },
            [ReservationStatus.Paid] = new[]
            {
                ReservationStatus.Completed,
                ReservationStatus.Cancelled
            },
            [ReservationStatus.Completed] = Array.Empty<ReservationStatus>(),
            [ReservationStatus.Cancelled] = Array.Empty<ReservationStatus>()
        };

    public static bool CanTransition(ReservationStatus from, ReservationStatus to)
        => from != to && Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<ReservationStatus> AllowedTargets(ReservationStatus from)
        => Allowed.TryGetValue(from, out var targets) ? targets : Array.Empty<ReservationStatus>();

    /// <summary>Statuses that still owe the gym money.</summary>
    public static bool IsUnpaid(ReservationStatus status)
        => status is ReservationStatus.Initial or ReservationStatus.PendingApproval or ReservationStatus.Approved;

    /// <summary>Statuses that occupy a seat in a training's capacity.</summary>
    public static bool OccupiesCapacity(ReservationStatus status)
        => status != ReservationStatus.Cancelled;
}
