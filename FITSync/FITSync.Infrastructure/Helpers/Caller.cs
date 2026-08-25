using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;

namespace FITSync.Infrastructure.Helpers
{
    public class Caller : ICaller
    {
        private readonly string? _userId;
        private readonly HttpContext? _httpContext;

        public Caller(IHttpContextAccessor httpContextAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _userId = _httpContext?.GetUserID();
        }

        public string? UserId => _userId;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_userId);

        public int? UserIdValue => int.TryParse(_userId, out var id) ? id : null;

        public bool IsAdministrator => IsInRole(RoleDefinition.Administrator);

        public bool IsInRole(string role) => _httpContext?.User?.IsInRole(role) ?? false;

        public int RequireUserId()
            => UserIdValue ?? throw new UnauthorizedAccessException("The request is not authenticated.");
    }
}
