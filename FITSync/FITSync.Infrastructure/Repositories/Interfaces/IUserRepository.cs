using FITSync.Domain.Models;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<(List<User> Items, int TotalCount)> SearchAsync(
            string? name, string? role, bool? enabled,
            int skip, int take, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
