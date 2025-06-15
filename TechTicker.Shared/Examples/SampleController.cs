using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Attributes;
using TechTicker.Shared.Common;

namespace TechTicker.SampleService.Controllers
{
    /// <summary>
    /// Sample controller demonstrating TechTicker authentication patterns
    /// </summary>
    [Route("api/[controller]")]
    public class SampleController : BaseApiController
    {
        private readonly ILogger<SampleController> _logger;

        public SampleController(ILogger<SampleController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Public endpoint (no authentication required)
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<object>> GetPublicData()
        {
            return Ok((object)new { message = "This is public data", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Read-only endpoint (requires authentication and read scope)
        /// </summary>
        [HttpGet("data")]
        [ReadOnlyAccess]
        public ActionResult<ApiResponse<object>> GetData()
        {
            _logger.LogInformation("User {UserId} ({Email}) accessed data endpoint",
                CurrentUserId, CurrentUserEmail);

            return Ok((object)new
            {
                message = "This is protected data",
                user = new
                {
                    id = CurrentUserId,
                    email = CurrentUserEmail,
                    roles = CurrentUserRoles,
                    scopes = CurrentUserScopes
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Write endpoint (requires admin role and write scope)
        /// </summary>
        [HttpPost("data")]
        [WriteAccess]
        public ActionResult<ApiResponse<object>> CreateData([FromBody] object data)
        {
            _logger.LogInformation("User {UserId} created data", CurrentUserId);

            return Created((object)new
            {
                message = "Data created successfully",
                createdBy = CurrentUserId,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Admin-only endpoint
        /// </summary>
        [HttpGet("admin")]
        [AdminOnly]
        public ActionResult<ApiResponse<object>> GetAdminData()
        {
            _logger.LogInformation("Admin {UserId} accessed admin endpoint", CurrentUserId);

            return Ok((object)new
            {
                message = "This is admin-only data",
                adminUser = CurrentUserEmail,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// User-specific data (users can only access their own data)
        /// </summary>
        [HttpGet("users/{userId}/data")]
        [UserOrAdmin]
        public ActionResult<ApiResponse<object>> GetUserData(string userId)
        {
            // Check if user can access this data
            if (!CanAccessUserData(userId))
            {
                var errorResponse = ApiResponse<object>.FailureResult("You do not have permission to access this user's data", 403);
                errorResponse.CorrelationId = CorrelationId;
                return StatusCode(403, errorResponse);
            }

            _logger.LogInformation("User {UserId} accessed data for user {TargetUserId}",
                CurrentUserId, userId);

            return Ok((object)new
            {
                message = $"Data for user {userId}",
                accessedBy = CurrentUserId,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Service-to-service endpoint (for inter-service communication)
        /// </summary>
        [HttpGet("internal")]
        [ServiceToService]
        public ActionResult<ApiResponse<object>> GetInternalData()
        {
            _logger.LogInformation("Service-to-service call made");

            return Ok((object)new
            {
                message = "This is internal service data",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Custom authorization policy endpoint
        /// </summary>
        [HttpGet("custom")]
        [TechTickerAuthorize(RequiredRoles = new[] { "CustomRole" }, RequiredScopes = new[] { "custom-scope" })]
        public ActionResult<ApiResponse<object>> GetCustomData()
        {
            return Ok((object)new
            {
                message = "This requires custom authorization",
                user = CurrentUserId,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Demonstrates manual authorization checking
        /// </summary>
        [HttpGet("manual-auth")]
        public ActionResult<ApiResponse<object>> GetDataWithManualAuth()
        {
            // Manual authentication check
            if (!IsAuthenticated)
            {
                return Unauthorized("Authentication required");
            }

            // Manual role check
            if (!IsAdmin && !IsInRole("SpecialUser"))
            {
                return Forbid("Insufficient permissions");
            }

            // Manual scope check
            if (!HasScope("special-access"))
            {
                return Forbid("Special access scope required");
            }

            return Ok((object)new
            {
                message = "Manual authorization successful",
                user = CurrentUserId,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
