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
        public IActionResult GetPublicData()
        {
            return Ok(new { message = "This is public data", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Read-only endpoint (requires authentication and read scope)
        /// </summary>
        [HttpGet("data")]
        [ReadOnlyAccess]
        public IActionResult GetData()
        {
            _logger.LogInformation("User {UserId} ({Email}) accessed data endpoint", 
                CurrentUserId, CurrentUserEmail);

            return Ok(new 
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
        public IActionResult CreateData([FromBody] object data)
        {
            _logger.LogInformation("User {UserId} created data", CurrentUserId);

            return Created(new 
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
        public IActionResult GetAdminData()
        {
            _logger.LogInformation("Admin {UserId} accessed admin endpoint", CurrentUserId);

            return Ok(new 
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
        public IActionResult GetUserData(string userId)
        {
            // Check if user can access this data
            var accessCheck = EnsureCanAccessUserData(userId);
            if (accessCheck != null) return accessCheck;

            _logger.LogInformation("User {UserId} accessed data for user {TargetUserId}", 
                CurrentUserId, userId);

            return Ok(new 
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
        public IActionResult GetInternalData()
        {
            _logger.LogInformation("Service-to-service call made");

            return Ok(new 
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
        public IActionResult GetCustomData()
        {
            return Ok(new 
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
        public IActionResult GetDataWithManualAuth()
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

            return Ok(new 
            { 
                message = "Manual authorization successful",
                user = CurrentUserId,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
