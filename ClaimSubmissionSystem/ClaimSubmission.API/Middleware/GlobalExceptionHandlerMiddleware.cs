using System.Net;
using System.Text.Json;

namespace ClaimSubmission.API.Middleware
{
    /// <summary>
    /// Global exception handling middleware to catch and format all unhandled exceptions
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception: {ex.GetType().Name} - {ex.Message}");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "An error occurred",
                details = exception.Message,
                exceptionType = exception.GetType().Name
            };

            // Determine status code based on exception type
            var statusCode = exception switch
            {
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException => HttpStatusCode.BadRequest,
                TimeoutException => HttpStatusCode.GatewayTimeout,
                _ when IsSqlException(exception) => 
                    IsSqlConnectionError(exception) ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.InternalServerError,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsJsonAsync(response);
        }

        private static bool IsSqlException(Exception ex)
        {
            return ex.GetType().Name == "SqlException" || 
                   ex.InnerException?.GetType().Name == "SqlException" ||
                   ex.Message.Contains("SQL Server") ||
                   ex.Message.Contains("database");
        }

        private static bool IsSqlConnectionError(Exception ex)
        {
            var message = ex.Message.ToLower();
            return message.Contains("connection") || 
                   message.Contains("timeout") || 
                   message.Contains("provider") || 
                   message.Contains("network") ||
                   message.Contains("tcp");
        }
    }
}
