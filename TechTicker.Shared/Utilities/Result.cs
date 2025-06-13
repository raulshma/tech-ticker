using TechTicker.Shared.Common;
using TechTicker.Shared.Exceptions;

namespace TechTicker.Shared.Utilities
{
    /// <summary>
    /// Result pattern implementation for handling operations that may succeed or fail
    /// </summary>
    /// <typeparam name="T">The type of data returned on success</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Indicates whether the operation failed
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// The data returned on success
        /// </summary>
        public T? Data { get; private set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Error code for categorizing the failure
        /// </summary>
        public string? ErrorCode { get; private set; }

        /// <summary>
        /// Exception that caused the failure (if any)
        /// </summary>
        public Exception? Exception { get; private set; }

        private Result(bool isSuccess, T? data, string? errorMessage, string? errorCode, Exception? exception)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            Exception = exception;
        }

        /// <summary>
        /// Creates a successful result with data
        /// </summary>
        public static Result<T> Success(T data)
        {
            return new Result<T>(true, data, null, null, null);
        }

        /// <summary>
        /// Creates a failed result with error message
        /// </summary>
        public static Result<T> Failure(string errorMessage, string? errorCode = null)
        {
            return new Result<T>(false, default, errorMessage, errorCode, null);
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        public static Result<T> Failure(Exception exception)
        {
            var errorCode = exception is TechTickerException techTickerEx ? techTickerEx.ErrorCode : "UNKNOWN_ERROR";
            return new Result<T>(false, default, exception.Message, errorCode, exception);
        }

        /// <summary>
        /// Converts the result to an ApiResponse
        /// </summary>
        public ApiResponse<T> ToApiResponse(int successStatusCode = 200, int failureStatusCode = 400)
        {
            if (IsSuccess)
            {
                return ApiResponse<T>.SuccessResult(Data!, statusCode: successStatusCode);
            }

            return ApiResponse<T>.FailureResult(ErrorMessage!, failureStatusCode);
        }

        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess && Data != null)
            {
                action(Data);
            }
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        public Result<T> OnFailure(Action<string> action)
        {
            if (IsFailure && ErrorMessage != null)
            {
                action(ErrorMessage);
            }
            return this;
        }

        /// <summary>
        /// Maps the result to a different type if successful
        /// </summary>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (IsSuccess && Data != null)
            {
                return Result<TNew>.Success(mapper(Data));
            }

            return Result<TNew>.Failure(ErrorMessage!, ErrorCode);
        }

        /// <summary>
        /// Binds the result to another operation if successful
        /// </summary>
        public async Task<Result<TNew>> BindAsync<TNew>(Func<T, Task<Result<TNew>>> binder)
        {
            if (IsSuccess && Data != null)
            {
                return await binder(Data);
            }

            return Result<TNew>.Failure(ErrorMessage!, ErrorCode);
        }
    }

    /// <summary>
    /// Non-generic result for operations that don't return data
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Indicates whether the operation failed
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Error code for categorizing the failure
        /// </summary>
        public string? ErrorCode { get; private set; }

        /// <summary>
        /// Exception that caused the failure (if any)
        /// </summary>
        public Exception? Exception { get; private set; }

        private Result(bool isSuccess, string? errorMessage, string? errorCode, Exception? exception)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            Exception = exception;
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static Result Success()
        {
            return new Result(true, null, null, null);
        }

        /// <summary>
        /// Creates a failed result with error message
        /// </summary>
        public static Result Failure(string errorMessage, string? errorCode = null)
        {
            return new Result(false, errorMessage, errorCode, null);
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        public static Result Failure(Exception exception)
        {
            var errorCode = exception is TechTickerException techTickerEx ? techTickerEx.ErrorCode : "UNKNOWN_ERROR";
            return new Result(false, exception.Message, errorCode, exception);
        }

        /// <summary>
        /// Converts the result to an ApiResponse
        /// </summary>
        public ApiResponse ToApiResponse(int successStatusCode = 200, int failureStatusCode = 400)
        {
            if (IsSuccess)
            {
                return ApiResponse.SuccessResult(statusCode: successStatusCode);
            }

            return ApiResponse.FailureResult(ErrorMessage!, failureStatusCode);
        }

        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        public Result OnSuccess(Action action)
        {
            if (IsSuccess)
            {
                action();
            }
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        public Result OnFailure(Action<string> action)
        {
            if (IsFailure && ErrorMessage != null)
            {
                action(ErrorMessage);
            }
            return this;
        }
    }
}
