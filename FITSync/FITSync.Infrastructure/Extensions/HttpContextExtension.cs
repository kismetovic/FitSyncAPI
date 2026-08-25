using FITSync.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace FITSync.Infrastructure.Extensions
{
    public static class HttpContextExtension
    {
        public static string? GetUserID(this HttpContext? httpContext)
        {
            return httpContext?.User?.Claims?.FirstOrDefault(c => c.Type == CustomClaimTypes.UserId)?.Value;
        }
    }
}
