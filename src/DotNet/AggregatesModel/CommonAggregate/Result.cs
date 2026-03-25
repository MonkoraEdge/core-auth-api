using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    /// <summary>
    /// Represents the result of a service/use-case operation without a return value.
    /// Use Result&lt;T&gt; when the operation returns data.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; protected init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage { get; protected init; }
        public ErrorMessageResponse Error { get; protected init; }

        protected Result() { }

        public static Result Success() => new() { IsSuccess = true };

        public static Result Failure(string message)
            => new() { IsSuccess = false, ErrorMessage = message };

        public static Result Failure(ErrorMessageResponse error)
            => new() { IsSuccess = false, Error = error, ErrorMessage = error?.Message };

        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(string message) => Result<T>.Failure(message);
        public static Result<T> Failure<T>(ErrorMessageResponse error) => Result<T>.Failure(error);

        /// <summary>
        /// Execute <paramref name="onSuccess"/> when IsSuccess, otherwise <paramref name="onFailure"/>.
        /// </summary>
        public TOut Match<TOut>(Func<TOut> onSuccess, Func<string, TOut> onFailure)
            => IsSuccess ? onSuccess() : onFailure(ErrorMessage);
    }

    /// <summary>
    /// Represents the result of a service/use-case operation with a typed return value.
    ///
    /// Usage:
    /// <code>
    /// // Service method
    /// public async Task&lt;Result&lt;OrderDto&gt;&gt; GetOrderAsync(Guid id)
    /// {
    ///     var order = await _repo.GetAsync(id);
    ///     if (order is null) return Result.Failure&lt;OrderDto&gt;("Order not found.");
    ///     return Result.Success(_mapper.Map&lt;OrderDto&gt;(order));
    /// }
    ///
    /// // Caller
    /// var result = await _service.GetOrderAsync(id);
    /// if (result.IsFailure) return NotFound(result.ErrorMessage);
    /// return Ok(result.Value);
    /// </code>
    /// </summary>
    public class Result<T> : Result
    {
        public T Value { get; private init; }

        private Result() { }

        public static Result<T> Success(T value)
            => new() { IsSuccess = true, Value = value };

        public static new Result<T> Failure(string message)
            => new() { IsSuccess = false, ErrorMessage = message };

        public static new Result<T> Failure(ErrorMessageResponse error)
            => new() { IsSuccess = false, Error = error, ErrorMessage = error?.Message };

        /// <summary>
        /// Execute <paramref name="onSuccess"/> when IsSuccess, otherwise <paramref name="onFailure"/>.
        /// </summary>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure)
            => IsSuccess ? onSuccess(Value) : onFailure(ErrorMessage);
    }
}
