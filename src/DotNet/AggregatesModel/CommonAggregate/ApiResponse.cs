using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    /// <summary>
    /// Standard HTTP response envelope for all API endpoints.
    ///
    /// Usage in controllers:
    /// <code>
    /// // Success
    /// return Ok(ApiResponse.Ok(orderDto));
    ///
    /// // Created
    /// return CreatedAtAction(..., ApiResponse.Created(new CreateResponse { Id = id }));
    ///
    /// // No content
    /// return Ok(ApiResponse.Ok());
    ///
    /// // Failure (from Result pattern)
    /// if (result.IsFailure) return BadRequest(ApiResponse.Fail(result.Error));
    /// return Ok(ApiResponse.Ok(result.Value));
    /// </code>
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>True when the operation completed without errors.</summary>
        public bool Success { get; init; }

        /// <summary>The response payload. Null on failure.</summary>
        public T Data { get; init; }

        /// <summary>Structured error information. Null on success.</summary>
        public ErrorMessageResponse Error { get; init; }

        /// <summary>Optional metadata (e.g. pagination summary).</summary>
        public object Metadata { get; init; }

        /// <summary>UTC timestamp of the response.</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Non-generic version for void responses (<c>200 OK</c> with no data payload).
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        // ----------------------------------------------------------------
        // Factory methods
        // ----------------------------------------------------------------

        /// <summary>Success with a typed data payload.</summary>
        public static ApiResponse<T> Ok<T>(T data, object metadata = null)
            => new() { Success = true, Data = data, Metadata = metadata };

        /// <summary>Success with no data payload.</summary>
        public static ApiResponse Ok()
            => new() { Success = true };

        /// <summary>Failure with a structured error.</summary>
        public static ApiResponse<T> Fail<T>(ErrorMessageResponse error)
            => new() { Success = false, Error = error };

        /// <summary>Failure with a plain message.</summary>
        public static ApiResponse<T> Fail<T>(string message)
            => new()
            {
                Success = false,
                Error = new ErrorMessageResponse
                {
                    Message = message,
                    OccurredAt = DateTime.UtcNow
                }
            };

        /// <summary>Success response for a paged result — includes pagination metadata.</summary>
        public static ApiResponse<IReadOnlyList<T>> Paged<T>(PaginatedList<T> paged)
            => new()
            {
                Success = true,
                Data = paged.Items,
                Metadata = new
                {
                    paged.TotalCount,
                    paged.TotalPages,
                    paged.PageIndex,
                    paged.HasPreviousPage,
                    paged.HasNextPage
                }
            };
    }
}
