using FITSync.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;

namespace FITSync.Infrastructure.Helpers
{
    public class Caller : ICaller
    {
        private readonly string? _userId;

        public Caller(IHttpContextAccessor httpContextAccessor)
        {
            _userId = httpContextAccessor.HttpContext?.GetUserID();
        }

        public string? UserId => _userId;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_userId);
    }
}
