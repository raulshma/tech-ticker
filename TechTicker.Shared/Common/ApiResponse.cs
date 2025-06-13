using System.Text.Json.Serialization;

namespace TechTicker.Shared.Common
{
    /// <summary>
    /// Generic API response wrapper for consistent response structure across all microservices
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The data payload of the response
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Additional error details or validation errors
        /// </summary>
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Timestamp when the response was generated
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Correlation ID for tracking requests across services
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Metadata for pagination, versioning, etc.
        /// </summary>
        public Dictionary<string, object>? Meta { get; set; }

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        public static ApiResponse<T> SuccessResult(T data, string? message = null, int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a successful response without data
        /// </summary>
        public static ApiResponse<T> SuccessResult(string? message = null, int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed response with error message
        /// </summary>
        public static ApiResponse<T> FailureResult(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed response with multiple errors
        /// </summary>
        public static ApiResponse<T> FailureResult(List<string> errors, string? message = null, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed response from an exception
        /// </summary>
        public static ApiResponse<T> FailureResult(Exception exception, int statusCode = 500)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = exception.Message,
                StatusCode = statusCode,
                Errors = new List<string> { exception.ToString() }
            };
        }
    }

    /// <summary>
    /// Non-generic API response for operations that don't return data
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        /// <summary>
        /// Creates a successful response without data
        /// </summary>
        public static new ApiResponse SuccessResult(string? message = null, int statusCode = 200)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed response with error message
        /// </summary>
        public static new ApiResponse FailureResult(string message, int statusCode = 400)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed response with multiple errors
        /// </summary>
        public static new ApiResponse FailureResult(List<string> errors, string? message = null, int statusCode = 400)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed response from an exception
        /// </summary>
        public static new ApiResponse FailureResult(Exception exception, int statusCode = 500)
        {
            return new ApiResponse
            {
                Success = false,
                Message = exception.Message,
                StatusCode = statusCode,
                Errors = new List<string> { exception.ToString() }
            };
        }
    }
}
