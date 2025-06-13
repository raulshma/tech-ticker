using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using TechTicker.Shared.Common;
using TechTicker.Shared.Exceptions;

namespace TechTicker.Shared.Middleware
{
    /// <summary>
    /// Global exception handling middleware for consistent error responses
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = GetOrCreateCorrelationId(context);
            
            _logger.LogError(exception, "An unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

            var response = CreateErrorResponse(exception, correlationId);
            
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static ApiResponse CreateErrorResponse(Exception exception, string correlationId)
        {
            return exception switch
            {
                NotFoundException notFound => new ApiResponse
                {
                    Success = false,
                    Message = notFound.Message,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    CorrelationId = correlationId,
                    Errors = new List<string> { notFound.ErrorCode }
                },

                ValidationException validation => new ApiResponse
                {
                    Success = false,
                    Message = validation.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    CorrelationId = correlationId,
                    Errors = validation.ValidationErrors.Select(e => $"{e.Field}: {e.Message}").ToList()
                },

                ConflictException conflict => new ApiResponse
                {
                    Success = false,
                    Message = conflict.Message,
                    StatusCode = (int)HttpStatusCode.Conflict,
                    CorrelationId = correlationId,
                    Errors = new List<string> { conflict.ErrorCode }
                },

                UnauthorizedException unauthorized => new ApiResponse
                {
                    Success = false,
                    Message = unauthorized.Message,
                    StatusCode = (int)HttpStatusCode.Forbidden,
                    CorrelationId = correlationId,
                    Errors = new List<string> { unauthorized.ErrorCode }
                },

                AuthenticationException authentication => new ApiResponse
                {
                    Success = false,
                    Message = authentication.Message,
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    CorrelationId = correlationId,
                    Errors = new List<string> { authentication.ErrorCode }
                },

                BusinessRuleException businessRule => new ApiResponse
                {
                    Success = false,
                    Message = businessRule.Message,
                    StatusCode = (int)HttpStatusCode.UnprocessableEntity,
                    CorrelationId = correlationId,
                    Errors = new List<string> { businessRule.ErrorCode }
                },

                ExternalServiceException externalService => new ApiResponse
                {
                    Success = false,
                    Message = "An external service is currently unavailable. Please try again later.",
                    StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                    CorrelationId = correlationId,
                    Errors = new List<string> { externalService.ErrorCode }
                },

                TechTickerException techTicker => new ApiResponse
                {
                    Success = false,
                    Message = techTicker.Message,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    CorrelationId = correlationId,
                    Errors = new List<string> { techTicker.ErrorCode }
                },

                _ => new ApiResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred. Please try again later.",
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    CorrelationId = correlationId,
                    Errors = new List<string> { "INTERNAL_SERVER_ERROR" }
                }
            };
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            const string correlationIdKey = "X-Correlation-ID";
            
            if (context.Request.Headers.TryGetValue(correlationIdKey, out var correlationId) && 
                !string.IsNullOrEmpty(correlationId))
            {
                return correlationId.ToString();
            }

            var newCorrelationId = Guid.NewGuid().ToString();
            context.Response.Headers.TryAdd(correlationIdKey, newCorrelationId);
            return newCorrelationId;
        }
    }
}
