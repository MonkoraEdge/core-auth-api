using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate
{
    public abstract class CustomHttpException : Exception
    {
        public string Module { get; }

        public ErrorCodeType ErrorCode { get; }

        public string ErrorMessage { get; }

        // Validation metadata
        public IList<ErrorField> Fields { get; }

        public IList<ErrorRow> Rows { get; }

        // Full error response object (optional)
        public ErrorMessageResponse ErrorMessageResponse { get; }

        // Correlation for distributed tracing
        public string CorrelationId { get; }

        private CustomHttpException(ErrorCodeType errorCode, string errorMessage, string correlationId = null)
        {
            ErrorCode = errorCode ?? ErrorCodeType.DOMAIN_RULE_VIOLATED;
            ErrorMessage = errorMessage ?? ErrorCode.Description;
            Fields = new List<ErrorField>();
            Rows = new List<ErrorRow>();
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
        }

        protected CustomHttpException(string module, ErrorCodeType errorCode, string errorMessage, string correlationId = null)
            : this(errorCode, errorMessage, correlationId)
        {
            Module = module;
        }

        protected CustomHttpException(string module, IList<ErrorField> fields, ErrorCodeType errorCode = null, string errorMessage = "", string correlationId = null)
            : this(errorCode, errorMessage, correlationId)
        {
            Module = module;
            Fields = fields ?? new List<ErrorField>();

            foreach (var field in Fields)
            {
                field.Error = string.IsNullOrWhiteSpace(field.Error) ? string.Empty : $"{field.Error}";
            }
        }

        protected CustomHttpException(string module, IList<ErrorRow> rows, ErrorCodeType errorCode = null, string errorMessage = "", string correlationId = null)
            : this(errorCode, errorMessage, correlationId)
        {
            Module = module;
            Rows = rows ?? new List<ErrorRow>();

            foreach (var row in Rows)
            {
                row.Error = string.IsNullOrWhiteSpace(row.Error) ? string.Empty : $"{row.Error}";
            }
        }

        protected CustomHttpException(string module, IList<ErrorField> fields, IList<ErrorRow> rows, ErrorCodeType errorCode = null, string errorMessage = "", string correlationId = null)
            : this(errorCode, errorMessage, correlationId)
        {
            Module = module;
            Fields = fields ?? new List<ErrorField>();
            Rows = rows ?? new List<ErrorRow>();

            foreach (var field in Fields)
            {
                field.Error = string.IsNullOrWhiteSpace(field.Error) ? string.Empty : $"{field.Error}";
            }

            foreach (var row in Rows)
            {
                row.Error = string.IsNullOrWhiteSpace(row.Error) ? string.Empty : $"{row.Error}";
            }
        }

        protected CustomHttpException(ErrorMessageResponse response)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            ErrorMessageResponse = response;
        }
    }
}
