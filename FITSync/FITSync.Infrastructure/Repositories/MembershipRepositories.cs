using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class MembershipPackageRepository : BaseRepository<MembershipPackage>, IMembershipPackageRepository
    {
        public MembershipPackageRepository(FitSyncDbContext context) : base(context)
        {
        }

        protected override IQueryable<MembershipPackage> BaseQuery()
            => _dbSet.Where(p => !p.IsDeleted).Include(p => p.TrainingType);

        public override async Task<List<MembershipPackage>> GetAsync()
            => await BaseQuery().OrderBy(p => p.Price).ToListAsync();

        public override async Task<MembershipPackage?> GetByIdAsync(int id)
            => await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<MembershipPackage>> GetActiveAsync(CancellationToken cancellationToken = default)
            => await BaseQuery().Where(p => p.IsActive).OrderBy(p => p.Price).ToListAsync(cancellationToken);
    }

    public class UserMembershipRepository : BaseRepository<UserMembership>, IUserMembershipRepository
    {
        public UserMembershipRepository(FitSyncDbContext context) : base(context)
        {
        }

        protected override IQueryable<UserMembership> BaseQuery()
            => _dbSet
                .Where(m => !m.IsDeleted)
                .Include(m => m.MembershipPackage).ThenInclude(p => p.TrainingType);

        public override async Task<UserMembership?> GetByIdAsync(int id)
            => await BaseQuery().FirstOrDefaultAsync(m => m.Id == id);

        public async Task<List<UserMembership>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.StartDate)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// The membership only if it really belongs to the caller and still has sessions
        /// left inside its validity window. A client cannot spend someone else's package.
        /// </summary>
        public async Task<UserMembership?> GetUsableAsync(int userId, int membershipId, DateTime moment, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(m =>
                    m.Id == membershipId &&
                    m.UserId == userId &&
                    m.Status == MembershipStatus.Active &&
                    moment >= m.StartDate &&
                    moment <= m.EndDate &&
                    m.SessionsUsed < m.SessionsTotal,
                    cancellationToken);
        }

        /// <summary>
        /// Best usable package for a training type: a type-specific package is preferred over
        /// a general one, and the one expiring soonest is spent first.
        /// </summary>
        public async Task<UserMembership?> FindUsableForTrainingTypeAsync(int userId, int trainingTypeId, DateTime moment, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(m =>
                    m.UserId == userId &&
                    m.Status == MembershipStatus.Active &&
                    moment >= m.StartDate &&
                    moment <= m.EndDate &&
                    m.SessionsUsed < m.SessionsTotal &&
                    (m.MembershipPackage.TrainingTypeId == null || m.MembershipPackage.TrainingTypeId == trainingTypeId))
                .OrderByDescending(m => m.MembershipPackage.TrainingTypeId != null)
                .ThenBy(m => m.EndDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>Flips packages whose window has passed to Expired, in one UPDATE.</summary>
        public async Task<int> ExpireOutdatedAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => !m.IsDeleted && m.Status == MembershipStatus.Active && m.EndDate < now)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, MembershipStatus.Expired), cancellationToken);
        }
    }
}
