using System.Text.Json;
using FITSync.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FITSync.WebAPI.Middleware
{
    /// <summary>
    /// Turns domain exceptions into meaningful HTTP responses with a stable error code.
    /// Business rule violations answer 4xx rather than a blanket 500, so the Flutter apps
    /// can react to the code and show the message the server produced.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        /// <summary>Nginx-style "client closed request". ASP.NET has no constant for it.</summary>
        private const int ClientClosedRequest = 499;

        /// <summary>Codes that describe a conflict with existing state rather than a bad request.</summary>
        private static readonly HashSet<string> ConflictCodes = new()
        {
            "TIME_CONFLICT",
            "CAPACITY_FULL",
            "ALREADY_PAID",
            "ALREADY_REVIEWED",
            "AVAILABILITY_OVERLAP",
            "INVALID_STATUS_TRANSITION"
        };

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleAsync(context, ex);
            }
        }

        private async Task HandleAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "An exception occurred after the response had already started.");
                throw exception;
            }

            var (status, code, message) = Map(exception);

            if (status >= StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception on {Method} {Path}.", context.Request.Method, context.Request.Path);
            else
                _logger.LogInformation("{Code} on {Method} {Path}: {Message}", code, context.Request.Method, context.Request.Path, message);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = new Dictionary<string, object?>
            {
                ["error"] = code,
                ["message"] = message
            };

            // Stack traces are useful locally and must never leak in production.
            if (status >= StatusCodes.Status500InternalServerError &&
                (_environment.IsDevelopment() || _environment.IsEnvironment("Docker")))
            {
                payload["detail"] = exception.ToString();
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }

        private static (int Status, string Code, string Message) Map(Exception exception) => exception switch
        {
            BusinessRuleException ex => (
                ConflictCodes.Contains(ex.Code) ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest,
                ex.Code,
                ex.Message),

            ForbiddenOperationException ex => (StatusCodes.Status403Forbidden, ex.Code, ex.Message),

            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Code, ex.Message),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication is required."),

            // A unique index rejected the write, e.g. a second capture for one reservation.
            DbUpdateException ex when IsUniqueViolation(ex) => (
                StatusCodes.Status409Conflict,
                "DUPLICATE_RECORD",
                "This record already exists and cannot be created twice."),

            HttpRequestException ex => (
                StatusCodes.Status503ServiceUnavailable,
                "EXTERNAL_SERVICE_UNAVAILABLE",
                $"An external service is currently unavailable: {ex.Message}"),

            OperationCanceledException => (ClientClosedRequest, "REQUEST_CANCELLED", "The request was cancelled."),

            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        private static bool IsUniqueViolation(DbUpdateException exception)
        {
            // 2601 = duplicate key row in an index, 2627 = unique constraint violation.
            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("2601") || message.Contains("2627") ||
                   message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
        }
    }
}
