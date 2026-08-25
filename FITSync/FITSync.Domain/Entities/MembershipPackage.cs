using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// A sellable monthly package: a fixed number of sessions valid for a fixed number of
    /// days, at a discount versus paying per session. This is what gives
    /// <c>ReservationType.Monthly</c> real business meaning.
    /// </summary>
    public class MembershipPackage : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>How long the package stays valid once purchased.</summary>
        public int DurationDays { get; set; } = 30;

        /// <summary>How many training sessions the package covers.</summary>
        public int SessionCount { get; set; }

        public decimal Price { get; set; }

        /// <summary>Optional restriction to a single training type; null means any type.</summary>
        public int? TrainingTypeId { get; set; }
        public virtual TrainingType? TrainingType { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<UserMembership> UserMemberships { get; set; } = new List<UserMembership>();
    }
}
