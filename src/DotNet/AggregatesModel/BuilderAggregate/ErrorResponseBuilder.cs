using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.BuilderAggregate
{
    public static class ErrorResponseBuilder
    {
        // ====================================================================
        // Build Response for CustomHttpException (all types)
        // ====================================================================
        public static ErrorMessageResponse Build(
            CustomHttpException ex,
            ILocalization localizer)
        {
            var localizedMessage = localizer?.GetText(ex.ErrorCode.Name);

            return new ErrorMessageResponse
            {
                Code = ex.ErrorCode.Name,
                Message = localizedMessage ?? ex.ErrorMessage,
                Module = ex.Module,
                Category = ex.ErrorCode.Category,
                Severity = ex.ErrorCode.Severity,
                Fields = ex.Fields.ToList(),
                Rows = ex.Rows.ToList(),
                CorrelationId = ex.CorrelationId,
                OccurredAt = DateTime.UtcNow,
                TraceId = GetTraceId(),
                SpanId = GetSpanId()
            };
        }

        // ====================================================================
        // Build Unexpected Exception Response (SystemError/Unexpected)
        // ====================================================================
        public static ErrorMessageResponse BuildUnexpected(
            Exception ex,
            ILocalization localizer)
        {
            var error = ErrorCodeType.UNEXPECTED;

            return new ErrorMessageResponse
            {
                Code = error.Name,
                Message = localizer?.GetText(error.Name) ?? error.Description,
                Module = "system",
                Category = error.Category,
                Severity = error.Severity,
                Fields = new List<ErrorField>(),
                Rows = new List<ErrorRow>(),
                CorrelationId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTime.UtcNow,
                TraceId = GetTraceId(),
                SpanId = GetSpanId(),
#if DEBUG
                StackTrace = ex.StackTrace
#endif
            };
        }

        // ====================================================================
        // Helpers for Distributed Tracing (OpenTelemetry)
        // ====================================================================

        private static string GetTraceId()
        {
            var activity = System.Diagnostics.Activity.Current;
            return activity?.TraceId.ToString();
        }

        private static string GetSpanId()
        {
            var activity = System.Diagnostics.Activity.Current;
            return activity?.SpanId.ToString();
        }
    }
}