using FITSync.Domain.Models;

namespace FITSync.Infrastructure.Authentication
{
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Returns the signed JWT. It used to return object and callers dug the value out
        /// with a dynamic cast, which silently produced a null token whenever the shape
        /// changed.
        /// </summary>
        string GenerateToken(User user);
    }
}
