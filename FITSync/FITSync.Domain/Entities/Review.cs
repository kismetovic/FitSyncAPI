using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    public class Review : BaseEntity
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public int TrainingId { get; set; }
        public virtual Training Training { get; set; } = null!;

        /// <summary>
        /// The attended reservation this review is about. Required, so a review can only
        /// exist for a training the user actually paid for and completed.
        /// </summary>
        public int ReservationId { get; set; }
        public virtual Reservation Reservation { get; set; } = null!;
    }
}
