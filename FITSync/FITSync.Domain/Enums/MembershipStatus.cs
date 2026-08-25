namespace FITSync.Domain.Enums;

public enum MembershipStatus
{
    /// <summary>Bought and paid for: sessions can be drawn down.</summary>
    Active = 0,
    Expired = 1,
    Cancelled = 2,

    /// <summary>
    /// Bought but not yet paid. Deliberately not usable: buying used to hand out an
    /// Active package on a single tap with no money involved at all.
    /// </summary>
    PendingPayment = 3
}
