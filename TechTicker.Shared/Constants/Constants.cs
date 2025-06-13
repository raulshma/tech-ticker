namespace TechTicker.Shared.Constants
{
    /// <summary>
    /// Common constants used across the application
    /// </summary>
    public static class ApplicationConstants
    {
        /// <summary>
        /// HTTP header names
        /// </summary>
        public static class Headers
        {
            public const string CorrelationId = "X-Correlation-ID";
            public const string RequestId = "X-Request-ID";
            public const string ApiVersion = "X-API-Version";
            public const string UserAgent = "User-Agent";
            public const string Authorization = "Authorization";
            public const string ContentType = "Content-Type";
        }

        /// <summary>
        /// Common error codes
        /// </summary>
        public static class ErrorCodes
        {
            public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
            public const string ValidationFailed = "VALIDATION_FAILED";
            public const string Conflict = "CONFLICT";
            public const string Unauthorized = "UNAUTHORIZED";
            public const string AuthenticationFailed = "AUTHENTICATION_FAILED";
            public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
            public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
            public const string InternalServerError = "INTERNAL_SERVER_ERROR";
            public const string UnknownError = "UNKNOWN_ERROR";
        }

        /// <summary>
        /// Pagination defaults
        /// </summary>
        public static class Pagination
        {
            public const int DefaultPageSize = 10;
            public const int MaxPageSize = 100;
            public const int DefaultPageNumber = 1;
        }

        /// <summary>
        /// Cache settings
        /// </summary>
        public static class Cache
        {
            public const int DefaultExpirationMinutes = 30;
            public const int ShortExpirationMinutes = 5;
            public const int LongExpirationMinutes = 60;
        }

        /// <summary>
        /// Database settings
        /// </summary>
        public static class Database
        {
            public const int DefaultCommandTimeoutSeconds = 30;
            public const int LongRunningCommandTimeoutSeconds = 300;
        }

        /// <summary>
        /// File upload settings
        /// </summary>
        public static class FileUpload
        {
            public const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
            public const int MaxFileNameLength = 255;
            
            public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".rtf" };
        }

        /// <summary>
        /// Validation settings
        /// </summary>
        public static class Validation
        {
            public const int MinPasswordLength = 8;
            public const int MaxPasswordLength = 128;
            public const int MaxNameLength = 100;
            public const int MaxDescriptionLength = 1000;
            public const int MaxEmailLength = 255;
            public const int MaxPhoneLength = 20;
            public const int MaxUrlLength = 2048;
        }

        /// <summary>
        /// Date and time formats
        /// </summary>
        public static class DateTimeFormats
        {
            public const string StandardDate = "yyyy-MM-dd";
            public const string StandardDateTime = "yyyy-MM-dd HH:mm:ss";
            public const string StandardDateTimeWithMilliseconds = "yyyy-MM-dd HH:mm:ss.fff";
            public const string Iso8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";
            public const string FriendlyDate = "MMMM dd, yyyy";
            public const string FriendlyDateTime = "MMMM dd, yyyy 'at' h:mm tt";
        }

        /// <summary>
        /// Regular expression patterns
        /// </summary>
        public static class RegexPatterns
        {
            public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            public const string PhoneNumber = @"^[\+]?[1-9][\d]{0,15}$";
            public const string AlphaNumeric = @"^[a-zA-Z0-9]+$";
            public const string AlphaNumericWithSpaces = @"^[a-zA-Z0-9\s]+$";
            public const string Slug = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
            public const string Guid = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
        }

        /// <summary>
        /// Content types
        /// </summary>
        public static class ContentTypes
        {
            public const string Json = "application/json";
            public const string Xml = "application/xml";
            public const string FormUrlEncoded = "application/x-www-form-urlencoded";
            public const string MultipartFormData = "multipart/form-data";
            public const string TextPlain = "text/plain";
            public const string TextHtml = "text/html";
            public const string ApplicationPdf = "application/pdf";
            public const string ImageJpeg = "image/jpeg";
            public const string ImagePng = "image/png";
            public const string ImageGif = "image/gif";
        }
    }

    /// <summary>
    /// Domain-specific constants for TechTicker
    /// </summary>
    public static class TechTickerConstants
    {
        /// <summary>
        /// Product-related constants
        /// </summary>
        public static class Products
        {
            public const int MaxNameLength = 255;
            public const int MaxManufacturerLength = 100;
            public const int MaxModelNumberLength = 100;
            public const int MaxSkuLength = 100;
            public const int MaxDescriptionLength = 2000;
        }

        /// <summary>
        /// Category-related constants
        /// </summary>
        public static class Categories
        {
            public const int MaxNameLength = 100;
            public const int MaxSlugLength = 100;
            public const int MaxDescriptionLength = 1000;
        }

        /// <summary>
        /// User-related constants
        /// </summary>
        public static class Users
        {
            public const int MaxFirstNameLength = 50;
            public const int MaxLastNameLength = 50;
            public const int MaxEmailLength = 255;
            public const int MaxPhoneLength = 20;
        }

        /// <summary>
        /// API versioning
        /// </summary>
        public static class ApiVersions
        {
            public const string V1 = "v1";
            public const string V2 = "v2";
            public const string Current = V1;
        }

        /// <summary>
        /// Service names for inter-service communication
        /// </summary>
        public static class Services
        {
            public const string ProductService = "product-service";
            public const string UserService = "user-service";
            public const string NotificationService = "notification-service";
            public const string InventoryService = "inventory-service";
        }
    }
}
