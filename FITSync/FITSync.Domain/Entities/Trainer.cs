using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// A coach who runs trainings. Separated from <see cref="User"/> because a trainer is a
    /// business record (bio, specialty, hourly surcharge) that exists independently of whether
    /// the person has a login account.
    /// </summary>
    public class Trainer : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Biography { get; set; }
        public string? Specialty { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Surcharge applied when a client books this trainer outside of the trainer's
        /// declared availability windows.
        /// </summary>
        public decimal OutsideAvailabilitySurcharge { get; set; }

        /// <summary>Optional link to a login account, when the trainer also uses the apps.</summary>
        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        public virtual ICollection<TrainerAvailability> Availabilities { get; set; } = new List<TrainerAvailability>();
        public virtual ICollection<Training> Trainings { get; set; } = new List<Training>();

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
