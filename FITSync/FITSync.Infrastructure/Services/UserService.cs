using AutoMapper;
using FITSync.Contracts.Common;
using FITSync.Contracts.Users;
using FITSync.Domain.Definitions;
using FITSync.Domain.Models;
using FITSync.Infrastructure.Exceptions;
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

        /// <summary>
        /// Admin-created users now actually get a role. Previously the account was created
        /// with no role at all, which meant the user could log in but every role-protected
        /// endpoint rejected them.
        /// </summary>
        public override async Task<UserResponse> InsertAsync(UserInsertRequest request)
        {
            var role = NormaliseRole(request.Role);

            var user = _mapper.Map<User>(request);
            user.Enabled = request.Enabled;
            // User overrides PhoneNumber as non-nullable, so the column is NOT NULL.
            // The request leaves it optional, which would otherwise fail on insert.
            user.PhoneNumber = request.PhoneNumber ?? string.Empty;

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new BusinessRuleException("USER_CREATE_FAILED", string.Join("; ", result.Errors.Select(e => e.Description)));

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                // Do not leave a half-created account with no role behind.
                await _userManager.DeleteAsync(user);
                throw new BusinessRuleException("ROLE_ASSIGN_FAILED", string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }

            var saved = await _userRepository.GetByIdAsync(user.Id);
            return _mapper.Map<UserResponse>(saved ?? user);
        }

        /// <summary>
        /// Updating a user can now also change the role, so the desktop role dropdown has a
        /// real effect instead of silently doing nothing.
        /// </summary>
        public override async Task<UserResponse?> UpdateAsync(int id, UserUpdateRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return null;

            if (!string.IsNullOrWhiteSpace(request.UserName)) user.UserName = request.UserName;
            if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = request.Email;
            if (request.Name != null) user.Name = request.Name;
            if (request.Surname != null) user.Surname = request.Surname;
            if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
            user.Enabled = request.Enabled;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new BusinessRuleException("USER_UPDATE_FAILED", string.Join("; ", updateResult.Errors.Select(e => e.Description)));

            if (!string.IsNullOrWhiteSpace(request.Role))
                await ApplyRoleAsync(user, NormaliseRole(request.Role));

            var saved = await _userRepository.GetByIdAsync(user.Id);
            return _mapper.Map<UserResponse>(saved ?? user);
        }

        public async Task<PagedResult<UserResponse>> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _userRepository.SearchAsync(
                request.Name, request.Role, request.Enabled, request.Skip, request.Take, cancellationToken);

            return PagedResult<UserResponse>.Create(
                _mapper.Map<List<UserResponse>>(items), request.Page, request.PageSize, total);
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

        /// <summary>Replaces whatever roles the user currently has with exactly the requested one.</summary>
        private async Task ApplyRoleAsync(User user, string role)
        {
            var current = await _userManager.GetRolesAsync(user);
            if (current.Count == 1 && current[0] == role)
                return;

            if (current.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, current);
                if (!removeResult.Succeeded)
                    throw new BusinessRuleException("ROLE_UPDATE_FAILED", string.Join("; ", removeResult.Errors.Select(e => e.Description)));
            }

            var addResult = await _userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
                throw new BusinessRuleException("ROLE_UPDATE_FAILED", string.Join("; ", addResult.Errors.Select(e => e.Description)));
        }

        private static string NormaliseRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return RoleDefinition.Client;

            if (string.Equals(role, RoleDefinition.Administrator, StringComparison.OrdinalIgnoreCase))
                return RoleDefinition.Administrator;

            if (string.Equals(role, RoleDefinition.Client, StringComparison.OrdinalIgnoreCase))
                return RoleDefinition.Client;

            throw new BusinessRuleException("INVALID_ROLE", $"Unknown role '{role}'. Allowed roles: Administrator, Client.");
        }
    }
}
