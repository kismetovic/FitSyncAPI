using AutoMapper;
using FITSync.Contracts.Users;
using FITSync.Domain.Models;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FITSync.Infrastructure.Services
{
    public class UserService : BaseCRUDService<User, UserResponse, UserInsertRequest, UserUpdateRequest>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;

        public UserService(IUserRepository repository, IMapper mapper, UserManager<User> userManager)
            : base(repository, mapper)
        {
            _userRepository = repository;
            _userManager = userManager;
        }

        public override async Task<UserResponse> InsertAsync(UserInsertRequest request)
        {
            var user = _mapper.Map<User>(request);
            user.Enabled = request.Enabled;
            if (string.IsNullOrEmpty(request.Password))
                throw new ArgumentException("Password is required for user creation.", nameof(request));
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            return _mapper.Map<UserResponse>(user);
        }

        public async Task<UserResponse?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            var entity = await _userRepository.GetByUserNameAsync(userName, cancellationToken);
            return entity == null ? null : _mapper.Map<UserResponse>(entity);
        }

        public async Task<UserResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var entity = await _userRepository.GetByEmailAsync(email, cancellationToken);
            return entity == null ? null : _mapper.Map<UserResponse>(entity);
        }
    }
}
