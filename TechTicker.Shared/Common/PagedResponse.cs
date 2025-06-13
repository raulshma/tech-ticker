namespace TechTicker.Shared.Common
{
    /// <summary>
    /// Pagination metadata for paginated responses
    /// </summary>
    public class PaginationMeta
    {
        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Number of the first item on current page
        /// </summary>
        public int FirstItemOnPage => (PageNumber - 1) * PageSize + 1;

        /// <summary>
        /// Number of the last item on current page
        /// </summary>
        public int LastItemOnPage => Math.Min(PageNumber * PageSize, (int)TotalCount);
    }    /// <summary>
    /// Paginated response wrapper
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public class PagedResponse<T>
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The data payload of the response
        /// </summary>
        public IEnumerable<T>? Data { get; set; }

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
        /// Pagination metadata
        /// </summary>
        public PaginationMeta Pagination { get; set; } = new();

        /// <summary>
        /// Creates a successful paginated response
        /// </summary>
        public static PagedResponse<T> SuccessResult(
            IEnumerable<T> data, 
            int pageNumber, 
            int pageSize, 
            long totalCount, 
            string? message = null)
        {
            return new PagedResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = 200,
                Pagination = new PaginationMeta
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                }
            };
        }

        /// <summary>
        /// Creates a failed paginated response
        /// </summary>
        public static PagedResponse<T> FailureResult(string message, int statusCode = 400)
        {
            return new PagedResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Data = Enumerable.Empty<T>(),
                Pagination = new PaginationMeta()
            };
        }
    }

    /// <summary>
    /// Standard pagination request parameters
    /// </summary>
    public class PaginationRequest
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        /// <summary>
        /// Page number (1-based, minimum 1)
        /// </summary>
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        /// <summary>
        /// Page size (minimum 1, maximum 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : value > 100 ? 100 : value;
        }

        /// <summary>
        /// Number of items to skip
        /// </summary>
        public int Skip => (PageNumber - 1) * PageSize;

        /// <summary>
        /// Number of items to take
        /// </summary>
        public int Take => PageSize;
    }
}
