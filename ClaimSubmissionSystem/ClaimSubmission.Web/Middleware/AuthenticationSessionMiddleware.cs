using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ClaimSubmission.Web.Middleware
{
    /// <summary>
    /// Middleware to handle session-based authentication and redirect unauthenticated users to login
    /// </summary>
    public class AuthenticationSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationSessionMiddleware> _logger;

        // Routes that don't require authentication
        private static readonly string[] AllowedRoutes = new[]
        {
            "/",
            "/home",
            "/home/index",
            "/authentication/login",
            "/authentication/register",
            "/css/",
            "/js/",
            "/lib/",
            "/images/",
            "/health",
            "/swagger"
        };

        public AuthenticationSessionMiddleware(RequestDelegate next, ILogger<AuthenticationSessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
            var isAllowedRoute = IsAllowedRoute(path);

            // If route is not allowed (protected), check if user is authenticated
            if (!isAllowedRoute)
            {
                var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";

                if (!isAuthenticated)
                {
                    _logger.LogWarning($"Unauthenticated access attempt to protected route: {path}");
                    context.Response.Redirect($"/Authentication/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
                    return;
                }

                var username = context.Session.GetString("Username");
                _logger.LogDebug($"Authenticated user '{username}' accessing route: {path}");
            }

            await _next(context);
        }

        /// <summary>
        /// Determines if a route requires authentication
        /// </summary>
        private static bool IsAllowedRoute(string path)
        {
            foreach (var route in AllowedRoutes)
            {
                if (path == route || path.StartsWith(route, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
