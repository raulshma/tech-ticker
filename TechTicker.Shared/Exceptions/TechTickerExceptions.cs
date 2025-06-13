namespace TechTicker.Shared.Exceptions
{
    /// <summary>
    /// Base exception for all TechTicker business logic exceptions
    /// </summary>
    public abstract class TechTickerException : Exception
    {
        /// <summary>
        /// Error code for categorizing the exception
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Additional context or metadata about the error
        /// </summary>
        public Dictionary<string, object>? Context { get; }

        protected TechTickerException(string message, string errorCode, Dictionary<string, object>? context = null) 
            : base(message)
        {
            ErrorCode = errorCode;
            Context = context;
        }

        protected TechTickerException(string message, string errorCode, Exception innerException, Dictionary<string, object>? context = null) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Context = context;
        }
    }

    /// <summary>
    /// Exception thrown when a requested resource is not found
    /// </summary>
    public class NotFoundException : TechTickerException
    {
        public NotFoundException(string resourceName, object identifier)
            : base($"{resourceName} with identifier '{identifier}' was not found.", "RESOURCE_NOT_FOUND",
                  new Dictionary<string, object> { { "ResourceName", resourceName }, { "Identifier", identifier } })
        {
        }

        public NotFoundException(string message)
            : base(message, "RESOURCE_NOT_FOUND")
        {
        }

        public NotFoundException(string message, Exception innerException)
            : base(message, "RESOURCE_NOT_FOUND", innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : TechTickerException
    {
        /// <summary>
        /// Collection of validation errors
        /// </summary>
        public List<ValidationError> ValidationErrors { get; }

        public ValidationException(string message, List<ValidationError> validationErrors)
            : base(message, "VALIDATION_FAILED", 
                  new Dictionary<string, object> { { "ValidationErrors", validationErrors } })
        {
            ValidationErrors = validationErrors;
        }

        public ValidationException(List<ValidationError> validationErrors)
            : this("One or more validation errors occurred.", validationErrors)
        {
        }

        public ValidationException(string field, string error)
            : this(new List<ValidationError> { new(field, error) })
        {
        }
    }

    /// <summary>
    /// Represents a single validation error
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// The field or property that failed validation
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// The validation error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The attempted value that failed validation
        /// </summary>
        public object? AttemptedValue { get; set; }

        public ValidationError(string field, string message, object? attemptedValue = null)
        {
            Field = field;
            Message = message;
            AttemptedValue = attemptedValue;
        }
    }

    /// <summary>
    /// Exception thrown when a business rule is violated
    /// </summary>
    public class BusinessRuleException : TechTickerException
    {
        public BusinessRuleException(string message, string? ruleCode = null)
            : base(message, ruleCode ?? "BUSINESS_RULE_VIOLATION")
        {
        }

        public BusinessRuleException(string message, string ruleCode, Exception innerException)
            : base(message, ruleCode, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a conflict occurs (e.g., duplicate resource)
    /// </summary>
    public class ConflictException : TechTickerException
    {
        public ConflictException(string message)
            : base(message, "CONFLICT")
        {
        }

        public ConflictException(string resourceName, string conflictReason)
            : base($"Conflict with {resourceName}: {conflictReason}", "CONFLICT",
                  new Dictionary<string, object> { { "ResourceName", resourceName }, { "ConflictReason", conflictReason } })
        {
        }

        public ConflictException(string message, Exception innerException)
            : base(message, "CONFLICT", innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when an operation is not authorized
    /// </summary>
    public class UnauthorizedException : TechTickerException
    {
        public UnauthorizedException(string message = "Access denied. You are not authorized to perform this operation.")
            : base(message, "UNAUTHORIZED")
        {
        }

        public UnauthorizedException(string message, Exception innerException)
            : base(message, "UNAUTHORIZED", innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when authentication fails
    /// </summary>
    public class AuthenticationException : TechTickerException
    {
        public AuthenticationException(string message = "Authentication failed.")
            : base(message, "AUTHENTICATION_FAILED")
        {
        }

        public AuthenticationException(string message, Exception innerException)
            : base(message, "AUTHENTICATION_FAILED", innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when external service communication fails
    /// </summary>
    public class ExternalServiceException : TechTickerException
    {
        /// <summary>
        /// Name of the external service that failed
        /// </summary>
        public string ServiceName { get; }

        public ExternalServiceException(string serviceName, string message)
            : base($"External service '{serviceName}' error: {message}", "EXTERNAL_SERVICE_ERROR",
                  new Dictionary<string, object> { { "ServiceName", serviceName } })
        {
            ServiceName = serviceName;
        }

        public ExternalServiceException(string serviceName, string message, Exception innerException)
            : base($"External service '{serviceName}' error: {message}", "EXTERNAL_SERVICE_ERROR", innerException,
                  new Dictionary<string, object> { { "ServiceName", serviceName } })
        {
            ServiceName = serviceName;
        }
    }
}
