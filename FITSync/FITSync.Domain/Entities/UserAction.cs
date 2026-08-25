using FITSync.Domain.Enums;
using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// Behavioural signal used by the recommender. Persisting these is what lets the
    /// recommender explain itself ("because you looked at similar trainings") instead of
    /// only reacting to completed reservations.
    /// </summary>
    public class UserAction : BaseEntity
    {
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public UserActionType ActionType { get; set; }

        public int? TrainingId { get; set; }
        public virtual Training? Training { get; set; }

        public int? TrainingTypeId { get; set; }
        public virtual TrainingType? TrainingType { get; set; }

        public string? Details { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
