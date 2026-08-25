namespace FITSync.Infrastructure.Helpers
{
    /// <summary>
    /// The authenticated caller, read from the JWT. Controllers and services use this
    /// instead of trusting ids that arrive in a request body.
    /// </summary>
    public interface ICaller
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }

        /// <summary>Numeric user id from the token, or null when unauthenticated.</summary>
        int? UserIdValue { get; }

        bool IsAdministrator { get; }

        bool IsInRole(string role);

        /// <summary>Numeric user id, or throws when the request is not authenticated.</summary>
        int RequireUserId();
    }
}
