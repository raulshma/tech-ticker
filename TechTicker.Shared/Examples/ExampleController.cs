using TechTicker.Shared.Common;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Exceptions;
using TechTicker.Shared.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace TechTicker.Shared.Examples
{
    /// <summary>
    /// Example controller demonstrating the usage of TechTicker.Shared components
    /// </summary>
    [Route("api/[controller]")]
    public class ExampleController : BaseApiController
    {
        /// <summary>
        /// Example of successful response with data
        /// </summary>
        [HttpGet("success")]
        public ActionResult<ApiResponse<object>> GetSuccessExample()
        {
            var data = new { Message = "Hello from TechTicker.Shared!", Timestamp = DateTime.UtcNow };
            return Ok((object)data, "Request processed successfully");
        }

        /// <summary>
        /// Example of paginated response
        /// </summary>
        [HttpGet("paginated")]
        public ActionResult<PagedResponse<object>> GetPaginatedExample([FromQuery] PaginationRequest request)
        {
            // Simulate data
            var items = Enumerable.Range(1, 100)
                .Skip(request.Skip)
                .Take(request.Take)
                .Select(i => (object)new { Id = i, Name = $"Item {i}" })
                .ToList();

            return OkPaged(items, request.PageNumber, request.PageSize, 100);
        }

        /// <summary>
        /// Example of using Result pattern
        /// </summary>
        [HttpGet("result/{id}")]
        public ActionResult<ApiResponse<object>> GetResultExample(int id)
        {
            var result = ProcessItem(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Example of throwing custom exceptions
        /// </summary>
        [HttpGet("error/{type}")]
        public IActionResult GetErrorExample(string type)
        {
            switch (type.ToLower())
            {
                case "notfound":
                    throw new NotFoundException("Item", $"item-{type}");
                case "validation":
                    throw new ValidationException("Name", "Name is required");
                case "conflict":
                    throw new ConflictException("Item", "Item already exists");
                case "business":
                    throw new BusinessRuleException("Cannot perform this operation during business hours");
                default:
                    throw new Exception("Unknown error type");
            }
        }

        /// <summary>
        /// Example of using validation utilities
        /// </summary>
        [HttpPost("validate")]
        public ActionResult<ApiResponse<string>> ValidateExample([FromBody] ValidationExampleRequest request)
        {
            var errors = new List<string>();

            if (!ValidationUtilities.IsValidEmail(request.Email))
                errors.Add("Invalid email format");

            if (!ValidationUtilities.IsValidPhoneNumber(request.Phone))
                errors.Add("Invalid phone number format");

            if (!ValidationUtilities.IsValidUrl(request.Website))
                errors.Add("Invalid website URL format");

            if (errors.Any())
            {
                var errorResponse = ApiResponse<string>.FailureResult(errors, "Validation failed", 400);
                errorResponse.CorrelationId = CorrelationId;
                return StatusCode(400, errorResponse);
            }

            var response = ApiResponse<string>.SuccessResult("All validations passed", null, 200);
            response.CorrelationId = CorrelationId;
            return StatusCode(200, response);
        }

        /// <summary>
        /// Example of using string utilities
        /// </summary>
        [HttpPost("utilities")]
        public ActionResult<ApiResponse<object>> UtilitiesExample([FromBody] UtilitiesExampleRequest request)
        {
            var result = new
            {
                Slug = StringUtilities.ToSlug(request.Text),
                MaskedEmail = StringUtilities.MaskSensitiveInfo(request.Email),
                Truncated = StringUtilities.Truncate(request.Text, 20),
                Hash = HashUtilities.GenerateSHA256(request.Text),
                RandomString = StringUtilities.GenerateRandomString(10)
            };

            return Ok((object)result);
        }

        private Result<object> ProcessItem(int id)
        {
            if (id <= 0)
                return Result<object>.Failure("Invalid ID", "VALIDATION_FAILED");

            if (id == 404)
                return Result<object>.Failure("Item not found", "RESOURCE_NOT_FOUND");

            if (id == 409)
                return Result<object>.Failure("Item conflict", "CONFLICT");

            return Result<object>.Success(new { Id = id, Name = $"Item {id}", ProcessedAt = DateTime.UtcNow });
        }
    }

    public class ValidationExampleRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
    }

    public class UtilitiesExampleRequest
    {
        public string Text { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
