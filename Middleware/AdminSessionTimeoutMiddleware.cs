using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ThesisRepository.Middleware
{
    /// <summary>
    /// Middleware to enforce admin session timeout.
    /// Admin sessions expire after a configured time period and require re-authentication.
    /// </summary>
    public class AdminSessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminSessionTimeoutMiddleware> _logger;

        public AdminSessionTimeoutMiddleware(RequestDelegate next, ILogger<AdminSessionTimeoutMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the request is to an admin-only endpoint
            if (IsAdminEndpoint(context.Request.Path))
            {
                var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value;

                // Only apply session timeout to admin users
                if (userRole == "admin" && context.User?.Identity?.IsAuthenticated == true)
                {
                    // Check if token is expired
                    var tokenExpiry = context.User?.FindFirst("exp")?.Value;
                    if (tokenExpiry != null && long.TryParse(tokenExpiry, out var expiryTimestamp))
                    {
                        var expiryDateTime = UnixTimeStampToDateTime(expiryTimestamp);
                        
                        if (DateTime.UtcNow > expiryDateTime)
                        {
                            _logger.LogWarning($"Admin session expired for user: {context.User?.FindFirst(ClaimTypes.Email)?.Value}");
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                message = "Admin session has expired. Please sign in again.",
                                code = "ADMIN_SESSION_EXPIRED"
                            });
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Determines if the current endpoint is admin-only.
        /// Add endpoints that require admin authorization here.
        /// </summary>
        private static bool IsAdminEndpoint(PathString path)
        {
            // Admin-specific endpoints
            var adminPaths = new[]
            {
                "/api/user",  // User management endpoints
                "/api/user/" // User management with ID
            };

            var pathValue = path.Value ?? "";

            return adminPaths.Any(adminPath => 
                pathValue.StartsWith(adminPath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Converts Unix timestamp (seconds since epoch) to DateTime.
        /// </summary>
        private static DateTime UnixTimeStampToDateTime(long unixTimestamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimestamp).ToUniversalTime();
            return dateTime;
        }
    }
}
