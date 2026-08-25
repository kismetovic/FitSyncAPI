using FITSync.Domain.Enums;
using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// One payment attempt against a reservation.
    /// A reservation may have several attempts, but only one may reach
    /// <see cref="PaymentStatus.Captured"/>; that rule is enforced in PaymentService and
    /// backed by a filtered unique index in FitSyncDbContext.
    /// </summary>
    public class Payment : BaseEntity
    {
        public decimal Amount { get; set; }

        /// <summary>Provider capture id. Empty until the payment is actually captured.</summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Provider order id (the PayPal order). Unique across the table so replaying the
        /// same capture callback cannot create a second payment row.
        /// </summary>
        public string? ProviderOrderId { get; set; }

        public string Currency { get; set; } = "BAM";
        public PaymentProvider PaymentProvider { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime? CapturedAt { get; set; }
        public string? FailureReason { get; set; }

        /// <summary>Admin/trainer who confirmed a cash payment. Null for online payments.</summary>
        public int? ConfirmedByUserId { get; set; }
        public virtual User? ConfirmedByUser { get; set; }

        /// <summary>
        /// A payment settles exactly one thing: either a reservation or a bought
        /// membership package. Both are nullable and exactly one is set; the database
        /// enforces that with a CHECK constraint, not just this comment.
        /// </summary>
        public int? ReservationId { get; set; }
        public virtual Reservation? Reservation { get; set; }

        public int? UserMembershipId { get; set; }
        public virtual UserMembership? UserMembership { get; set; }
    }
}
