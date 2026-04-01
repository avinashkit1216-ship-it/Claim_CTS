// OperationResult.cs - Standardized API Response Pattern
using System.Text.Json.Serialization;

namespace ClaimSubmission.API.Common
{
    /// <summary>
    /// Standardized operation result for all API responses
    /// Ensures consistency across all endpoints
    /// </summary>
    public class OperationResult<T>
    {
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public int StatusCode { get; set; }

        /// <summary>
        /// Create successful operation result
        /// </summary>
        public static OperationResult<T> Success(T data, int statusCode = StatusCodes.Status200OK) =>
            new()
            {
                IsSuccess = true,
                Data = data,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };

        /// <summary>
        /// Create failed operation result
        /// </summary>
        public static OperationResult<T> Failure(string error, int statusCode = StatusCodes.Status400BadRequest) =>
            new()
            {
                IsSuccess = false,
                Error = error,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };
    }

    /// <summary>
    /// Non-generic version for operations that don't return data
    /// </summary>
    public class OperationResult
    {
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public int StatusCode { get; set; }

        public static OperationResult Success(int statusCode = StatusCodes.Status200OK) =>
            new()
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };

        public static OperationResult Failure(string error, int statusCode = StatusCodes.Status400BadRequest) =>
            new()
            {
                IsSuccess = false,
                Error = error,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };
    }
}
