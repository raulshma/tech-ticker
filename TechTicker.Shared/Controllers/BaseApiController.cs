using Microsoft.AspNetCore.Mvc;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Shared.Controllers
{
    /// <summary>
    /// Base controller providing common functionality for all API controllers
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Gets the correlation ID from the request headers
        /// </summary>
        protected string CorrelationId => 
            Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) 
                ? correlationId.ToString() 
                : Guid.NewGuid().ToString();

        /// <summary>
        /// Creates an OK response with the specified data
        /// </summary>
        protected IActionResult Ok<T>(T data, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResult(data, message);
            response.CorrelationId = CorrelationId;
            return base.Ok(response);
        }

        /// <summary>
        /// Creates an OK response without data
        /// </summary>
        protected IActionResult Ok(string? message = null)
        {
            var response = ApiResponse.SuccessResult(message);
            response.CorrelationId = CorrelationId;
            return base.Ok(response);
        }

        /// <summary>
        /// Creates a Created response with the specified data
        /// </summary>
        protected IActionResult Created<T>(T data, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResult(data, message, 201);
            response.CorrelationId = CorrelationId;
            return StatusCode(201, response);
        }

        /// <summary>
        /// Creates a Created response without data
        /// </summary>
        protected IActionResult Created(string? message = null)
        {
            var response = ApiResponse.SuccessResult(message, 201);
            response.CorrelationId = CorrelationId;
            return StatusCode(201, response);
        }

        /// <summary>
        /// Creates a BadRequest response with error message
        /// </summary>
        protected IActionResult BadRequest(string message)
        {
            var response = ApiResponse.FailureResult(message, 400);
            response.CorrelationId = CorrelationId;
            return base.BadRequest(response);
        }

        /// <summary>
        /// Creates a BadRequest response with validation errors
        /// </summary>
        protected IActionResult BadRequest(List<string> errors, string? message = null)
        {
            var response = ApiResponse.FailureResult(errors, message, 400);
            response.CorrelationId = CorrelationId;
            return base.BadRequest(response);
        }

        /// <summary>
        /// Creates a NotFound response with error message
        /// </summary>
        protected IActionResult NotFound(string message)
        {
            var response = ApiResponse.FailureResult(message, 404);
            response.CorrelationId = CorrelationId;
            return base.NotFound(response);
        }

        /// <summary>
        /// Creates a Conflict response with error message
        /// </summary>
        protected IActionResult Conflict(string message)
        {
            var response = ApiResponse.FailureResult(message, 409);
            response.CorrelationId = CorrelationId;
            return StatusCode(409, response);
        }

        /// <summary>
        /// Creates an UnprocessableEntity response with error message
        /// </summary>
        protected IActionResult UnprocessableEntity(string message)
        {
            var response = ApiResponse.FailureResult(message, 422);
            response.CorrelationId = CorrelationId;
            return StatusCode(422, response);
        }        /// <summary>
        /// Returns a PagedResponse directly without double wrapping
        /// </summary>
        protected IActionResult OkPaged<T>(IEnumerable<T> data, int pageNumber, int pageSize, long totalCount, string? message = null)
        {
            var response = PagedResponse<T>.SuccessResult(data, pageNumber, pageSize, totalCount, message);
            response.CorrelationId = CorrelationId;
            return StatusCode(200, response);
        }

        /// <summary>
        /// Returns a PagedResponse directly without double wrapping
        /// </summary>
        protected IActionResult OkPagedDirect<T>(PagedResponse<T> pagedResponse)
        {
            pagedResponse.CorrelationId = CorrelationId;
            return StatusCode(200, pagedResponse);
        }

        /// <summary>
        /// Converts a Result to an appropriate HTTP response
        /// </summary>
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return result.ErrorCode switch
            {
                "RESOURCE_NOT_FOUND" => NotFound(result.ErrorMessage!),
                "VALIDATION_FAILED" => BadRequest(result.ErrorMessage!),
                "CONFLICT" => Conflict(result.ErrorMessage!),
                "BUSINESS_RULE_VIOLATION" => UnprocessableEntity(result.ErrorMessage!),
                _ => BadRequest(result.ErrorMessage!)
            };
        }

        /// <summary>
        /// Converts a non-generic Result to an appropriate HTTP response
        /// </summary>
        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
            {
                return Ok();
            }

            return result.ErrorCode switch
            {
                "RESOURCE_NOT_FOUND" => NotFound(result.ErrorMessage!),
                "VALIDATION_FAILED" => BadRequest(result.ErrorMessage!),
                "CONFLICT" => Conflict(result.ErrorMessage!),
                "BUSINESS_RULE_VIOLATION" => UnprocessableEntity(result.ErrorMessage!),
                _ => BadRequest(result.ErrorMessage!)
            };
        }
    }
}
