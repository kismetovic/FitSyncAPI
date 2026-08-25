using FITSync.Domain.Enums;
using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    public class Reservation : BaseEntity
    {
        public DateTime ReservationDate { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Initial;
        public ReservationType ReservationType { get; set; } = ReservationType.OneTime;
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public int TrainingId { get; set; }
        public virtual Training Training { get; set; } = null!;

        /// <summary>
        /// Price frozen at booking time: training price + additional services + any
        /// outside-availability surcharge. The payment flow charges this, never a
        /// client-supplied amount.
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>True when the slot falls outside the trainer's declared availability.</summary>
        public bool IsOutsideTrainerAvailability { get; set; }

        /// <summary>Surcharge component of <see cref="TotalPrice"/> for out-of-hours bookings.</summary>
        public decimal OutsideAvailabilitySurcharge { get; set; }

        /// <summary>Set when the reservation draws a session from a monthly package.</summary>
        public int? UserMembershipId { get; set; }
        public virtual UserMembership? UserMembership { get; set; }

        // --- Cancellation audit (a cancelled reservation stays in the system, it is never deleted) ---
        public DateTime? CancelledAt { get; set; }
        public int? CancelledByUserId { get; set; }
        public virtual User? CancelledByUser { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime? CompletedAt { get; set; }

        public virtual ICollection<ReservationService> ReservationServices { get; set; } = new List<ReservationService>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<ReservationStatusHistory> StatusHistory { get; set; } = new List<ReservationStatusHistory>();
    }
}
