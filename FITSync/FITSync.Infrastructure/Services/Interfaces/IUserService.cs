using FITSync.Contracts.Users;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IUserService : IBaseCRUDService<UserResponse, UserInsertRequest, UserUpdateRequest>
    {
        Task<UserResponse?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
