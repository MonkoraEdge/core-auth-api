using System.Text.Json.Serialization;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel
{
    public class ErrorMessageResponse
    {
        // -----------------------------
        // BASIC ERROR INFORMATION
        // -----------------------------

        /// <summary>
        /// Error code (ERR_XXXX)
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Human-readable error message (localized)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The functional module where the error occurred
        /// (e.g., "AuthService", "TransactionService")
        /// </summary>
        public string Module { get; set; }
        public string SubModule { get; set; }

        // -----------------------------
        // CLASSIFICATION & SEVERITY
        // -----------------------------

        /// <summary>
        /// Error category (Validation, Auth, System, Banking, etc.)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Severity level (Critical, Error, Warning, Info)
        /// </summary>
        public string Severity { get; set; }

        // -----------------------------
        // ADDITIONAL DETAILS
        // -----------------------------

        /// <summary>
        /// List of field-level validation errors
        /// </summary>
        public IList<ErrorField> Fields { get; set; } = new List<ErrorField>();

        /// <summary>
        /// List of row-level validation errors (Excel, Bulk Import)
        /// </summary>
        public IList<ErrorRow> Rows { get; set; } = new List<ErrorRow>();

        /// <summary>
        /// Optional: current correlation id (for distributed tracing)
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Optional: internal reference id for debugging
        /// </summary>
        public string ReferenceId { get; set; }

        // -----------------------------
        // HTTP & PATH
        // -----------------------------

        public int? HttpStatus { get; set; }

        /// <summary>
        /// API endpoint that caused the error
        /// </summary>
        public string Path { get; set; }

        public DateTime OccurredAt { get; set; }

        // For Distributed Tracing
        public string TraceId { get; set; }

        public string SpanId { get; set; }

        // Stack trace only available in DEBUG mode
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string StackTrace { get; set; }
    }
}
    