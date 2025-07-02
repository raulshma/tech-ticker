using Microsoft.AspNetCore.Mvc;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;
using System.Security.Claims;

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
        /// Gets the current user ID from the authenticated user
        /// </summary>
        protected string? CurrentUserId => 
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value ??
            User.FindFirst("id")?.Value ??
            HttpContext.Items["UserId"]?.ToString();

        /// <summary>
        /// Gets the current user email from the authenticated user
        /// </summary>
        protected string? CurrentUserEmail =>
            User.FindFirst(ClaimTypes.Email)?.Value ??
            User.FindFirst("email")?.Value ??
            HttpContext.Items["UserEmail"]?.ToString();

        /// <summary>
        /// Gets the current user name from the authenticated user
        /// </summary>
        protected string? CurrentUserName =>
            User.FindFirst(ClaimTypes.Name)?.Value ??
            User.FindFirst("name")?.Value ??
            User.FindFirst("preferred_username")?.Value ??
            HttpContext.Items["UserName"]?.ToString();

        /// <summary>
        /// Gets the current user roles from the authenticated user
        /// </summary>
        protected IEnumerable<string> CurrentUserRoles =>
            User.FindAll(ClaimTypes.Role)
                .Concat(User.FindAll("role"))
                .Select(c => c.Value)
                .Concat((HttpContext.Items["UserRoles"] as IEnumerable<string>) ?? Enumerable.Empty<string>())
                .Distinct();

        /// <summary>
        /// Gets the current user scopes from the authenticated user
        /// </summary>
        protected IEnumerable<string> CurrentUserScopes =>
            User.FindAll("scope")
                .Select(c => c.Value)
                .Concat((HttpContext.Items["UserScopes"] as IEnumerable<string>) ?? Enumerable.Empty<string>())
                .Distinct();

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        /// <summary>
        /// Checks if the current user has the specified role
        /// </summary>
        protected bool IsInRole(string role) => CurrentUserRoles.Contains(role);

        /// <summary>
        /// Checks if the current user is an admin
        /// </summary>
        protected bool IsAdmin => IsInRole("Admin");

        /// <summary>
        /// Checks if the current user has the specified scope
        /// </summary>
        protected bool HasScope(string scope) => CurrentUserScopes.Contains(scope);

        /// <summary>
        /// Checks if the current user can access the specified user's data
        /// </summary>
        protected bool CanAccessUserData(string targetUserId)
        {
            if (IsAdmin) return true;
            if (string.IsNullOrEmpty(CurrentUserId)) return false;
            return CurrentUserId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ensures the current user can access the specified user's data, returns Forbid if not
        /// </summary>
        protected IActionResult? EnsureCanAccessUserData(string targetUserId)
        {
            if (!CanAccessUserData(targetUserId))
            {
                return Forbid("You do not have permission to access this user's data");
            }
            return null;
        }

        /// <summary>
        /// Creates an OK response with the specified data
        /// </summary>
        protected ActionResult<ApiResponse<T>> Ok<T>(T data, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResult(data, message);
            response.CorrelationId = CorrelationId;
            return base.Ok(response);
        }

        /// <summary>
        /// Creates an OK response without data
        /// </summary>
        protected ActionResult<ApiResponse> Ok(string? message = null)
        {
            var response = ApiResponse.SuccessResult(message);
            response.CorrelationId = CorrelationId;
            return base.Ok(response);
        }

        /// <summary>
        /// Creates a Created response with the specified data
        /// </summary>
        protected ActionResult<ApiResponse<T>> Created<T>(T data, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResult(data, message, 201);
            response.CorrelationId = CorrelationId;
            return StatusCode(201, response);
        }

        /// <summary>
        /// Creates a Created response without data
        /// </summary>
        protected ActionResult<ApiResponse> Created(string? message = null)
        {
            var response = ApiResponse.SuccessResult(message, 201);
            response.CorrelationId = CorrelationId;
            return StatusCode(201, response);
        }

        /// <summary>
        /// Creates a BadRequest response with error message
        /// </summary>
        protected ActionResult<ApiResponse> BadRequest(string message)
        {
            var response = ApiResponse.FailureResult(message, 400);
            response.CorrelationId = CorrelationId;
            return base.BadRequest(response);
        }

        /// <summary>
        /// Creates a BadRequest response with validation errors
        /// </summary>
        protected ActionResult<ApiResponse> BadRequest(List<string> errors, string? message = null)
        {
            var response = ApiResponse.FailureResult(errors, message, 400);
            response.CorrelationId = CorrelationId;
            return base.BadRequest(response);
        }

        /// <summary>
        /// Creates a NotFound response with error message
        /// </summary>
        protected ActionResult<ApiResponse> NotFound(string message)
        {
            var response = ApiResponse.FailureResult(message, 404);
            response.CorrelationId = CorrelationId;
            return base.NotFound(response);
        }

        /// <summary>
        /// Creates a Conflict response with error message
        /// </summary>
        protected ActionResult<ApiResponse> Conflict(string message)
        {
            var response = ApiResponse.FailureResult(message, 409);
            response.CorrelationId = CorrelationId;
            return StatusCode(409, response);
        }

        /// <summary>
        /// Creates an UnprocessableEntity response with error message
        /// </summary>
        protected ActionResult<ApiResponse> UnprocessableEntity(string message)
        {
            var response = ApiResponse.FailureResult(message, 422);
            response.CorrelationId = CorrelationId;
            return StatusCode(422, response);
        }

        /// <summary>
        /// Creates a generic BadRequest response with error message
        /// </summary>
        protected ActionResult<ApiResponse<T>> BadRequestGeneric<T>(string message)
        {
            var response = ApiResponse<T>.FailureResult(message, 400);
            response.CorrelationId = CorrelationId;
            return base.BadRequest(response);
        }

        /// <summary>
        /// Creates a generic NotFound response with error message
        /// </summary>
        protected ActionResult<ApiResponse<T>> NotFoundGeneric<T>(string message)
        {
            var response = ApiResponse<T>.FailureResult(message, 404);
            response.CorrelationId = CorrelationId;
            return base.NotFound(response);
        }

        /// <summary>
        /// Creates a generic Conflict response with error message
        /// </summary>
        protected ActionResult<ApiResponse<T>> ConflictGeneric<T>(string message)
        {
            var response = ApiResponse<T>.FailureResult(message, 409);
            response.CorrelationId = CorrelationId;
            return StatusCode(409, response);
        }

        /// <summary>
        /// Creates a generic UnprocessableEntity response with error message
        /// </summary>
        protected ActionResult<ApiResponse<T>> UnprocessableEntityGeneric<T>(string message)
        {
            var response = ApiResponse<T>.FailureResult(message, 422);
            response.CorrelationId = CorrelationId;
            return StatusCode(422, response);
        }

        /// <summary>
        /// Returns a PagedResponse directly without double wrapping
        /// </summary>
        protected ActionResult<PagedResponse<T>> OkPaged<T>(IEnumerable<T> data, int pageNumber, int pageSize, long totalCount, string? message = null)
        {
            var response = PagedResponse<T>.SuccessResult(data, pageNumber, pageSize, totalCount, message);
            response.CorrelationId = CorrelationId;
            return StatusCode(200, response);
        }

        /// <summary>
        /// Returns a PagedResponse directly without double wrapping
        /// </summary>
        protected ActionResult<PagedResponse<T>> OkPagedDirect<T>(PagedResponse<T> pagedResponse)
        {
            pagedResponse.CorrelationId = CorrelationId;
            return StatusCode(200, pagedResponse);
        }

        /// <summary>
        /// Converts a Result to an appropriate HTTP response
        /// </summary>
        protected ActionResult<ApiResponse<T>> HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Data!);
            }

            return result.ErrorCode switch
            {
                "RESOURCE_NOT_FOUND" => base.NotFound(ApiResponse<T>.FailureResult(result.ErrorMessage!, 404)),
                "VALIDATION_FAILED" => base.BadRequest(ApiResponse<T>.FailureResult(result.ErrorMessage!, 400)),
                "CONFLICT" => StatusCode(409, ApiResponse<T>.FailureResult(result.ErrorMessage!, 409)),
                "BUSINESS_RULE_VIOLATION" => StatusCode(422, ApiResponse<T>.FailureResult(result.ErrorMessage!, 422)),
                _ => base.BadRequest(ApiResponse<T>.FailureResult(result.ErrorMessage!, 400))
            };
        }

        /// <summary>
        /// Converts a non-generic Result to an appropriate HTTP response
        /// </summary>
        protected ActionResult<ApiResponse> HandleResult(Result result)
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

        /// <summary>
        /// Converts a Result containing PagedResponse to an appropriate HTTP response
        /// </summary>
        protected ActionResult<PagedResponse<T>> HandlePagedResult<T>(Result<PagedResponse<T>> result)
        {
            if (result.IsSuccess)
            {
                result.Data!.CorrelationId = CorrelationId;
                return base.Ok(result.Data);
            }

            var errorResponse = PagedResponse<T>.FailureResult(result.ErrorMessage!, result.ErrorCode switch
            {
                "RESOURCE_NOT_FOUND" => 404,
                "VALIDATION_FAILED" => 400,
                "CONFLICT" => 409,
                "BUSINESS_RULE_VIOLATION" => 422,
                _ => 400
            });
            errorResponse.CorrelationId = CorrelationId;
            return errorResponse.StatusCode switch
            {
                400 => base.BadRequest(errorResponse),
                404 => base.NotFound(errorResponse),
                409 => StatusCode(409, errorResponse),
                422 => StatusCode(422, errorResponse),
                _ => base.BadRequest(errorResponse)
            };
        }
    }
}
