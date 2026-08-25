using FITSync.Domain.Enums;
using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// Append-only audit of every state-machine transition a reservation went through.
    /// Nothing in the system mutates a reservation's status without writing one of these.
    /// </summary>
    public class ReservationStatusHistory : BaseEntity
    {
        public int ReservationId { get; set; }
        public virtual Reservation Reservation { get; set; } = null!;

        public ReservationStatus FromStatus { get; set; }
        public ReservationStatus ToStatus { get; set; }

        /// <summary>Who performed the transition. Null for system-driven changes.</summary>
        public int? ChangedByUserId { get; set; }
        public virtual User? ChangedByUser { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? Reason { get; set; }
    }
}
