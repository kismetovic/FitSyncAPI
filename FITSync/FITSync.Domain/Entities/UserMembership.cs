using FITSync.Domain.Enums;
using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// A concrete package a user bought: a validity period and a session counter that
    /// monthly reservations draw down.
    /// </summary>
    public class UserMembership : BaseEntity
    {
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int MembershipPackageId { get; set; }
        public virtual MembershipPackage MembershipPackage { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int SessionsTotal { get; set; }
        public int SessionsUsed { get; set; }

        public MembershipStatus Status { get; set; } = MembershipStatus.Active;

        public decimal PricePaid { get; set; }

        public int SessionsRemaining => Math.Max(0, SessionsTotal - SessionsUsed);

        public bool IsUsableAt(DateTime moment)
            => Status == MembershipStatus.Active
               && !IsDeleted
               && moment >= StartDate
               && moment <= EndDate
               && SessionsRemaining > 0;
    }
}
