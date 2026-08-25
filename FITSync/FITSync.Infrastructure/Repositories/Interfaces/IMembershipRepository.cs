using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IMembershipPackageRepository : IBaseRepository<MembershipPackage>
    {
        Task<List<MembershipPackage>> GetActiveAsync(CancellationToken cancellationToken = default);
    }

    public interface IUserMembershipRepository : IBaseRepository<UserMembership>
    {
        Task<List<UserMembership>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<UserMembership?> GetUsableAsync(int userId, int membershipId, DateTime moment, CancellationToken cancellationToken = default);
        Task<UserMembership?> FindUsableForTrainingTypeAsync(int userId, int trainingTypeId, DateTime moment, CancellationToken cancellationToken = default);
        Task<int> ExpireOutdatedAsync(DateTime now, CancellationToken cancellationToken = default);
    }
}
