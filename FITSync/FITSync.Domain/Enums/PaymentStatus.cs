namespace FITSync.Domain.Enums;

/// <summary>
/// Lifecycle of a single payment attempt. A reservation may accumulate several
/// attempts (a failed PayPal order followed by a successful one), but at most one
/// of them may ever reach <see cref="Captured"/>.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Captured = 1,
    Failed = 2,
    Refunded = 3
}
