using FITSync.Domain.Enums;
using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    public class Training : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public int MaxCapacity { get; set; }
        public TrainingDifficulty Difficulty { get; set; }
        public int TrainingTypeId { get; set; }
        public virtual TrainingType TrainingType { get; set; } = null!;

        public int? TrainerId { get; set; }
        public virtual Trainer? Trainer { get; set; }

        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
