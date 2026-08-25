using FITSync.Contracts.Common;
using FITSync.Contracts.Memberships;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IMembershipService : IBaseCRUDService<MembershipPackageResponse, MembershipPackageInsertRequest, MembershipPackageUpdateRequest>
    {
        Task<List<MembershipPackageResponse>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<UserMembershipResponse>> GetMyMembershipsAsync(int userId, PagedRequest paging, CancellationToken cancellationToken = default);
        Task<UserMembershipResponse> PurchaseAsync(int userId, PurchaseMembershipRequest request, CancellationToken cancellationToken = default);
        Task<UserMembershipResponse?> GetUserMembershipAsync(int userId, int membershipId, CancellationToken cancellationToken = default);
        Task<UserMembershipResponse> CancelAsync(int callerUserId, int membershipId, CancellationToken cancellationToken = default);
    }
}
