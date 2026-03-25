using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate
{
    public class CustomHttpForbiddenException : CustomHttpException
    {
        public CustomHttpForbiddenException(
              string module,
              ErrorCodeType errorCode,
              string errorMessage,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: errorCode,
                  errorMessage: errorMessage,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Convenience constructor for common bad request with no fields
        // ----------------------------------------------------------
        public CustomHttpForbiddenException(
              string module,
              ErrorCodeType errorCode,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: errorCode,
                  errorMessage: errorCode.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Validation constructor with message only
        // ----------------------------------------------------------
        public CustomHttpForbiddenException(
              string module,
              string errorMessage,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: ErrorCodeType.FORBIDDEN,
                  errorMessage: errorMessage,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Validation constructor with fields only
        // ----------------------------------------------------------
        public CustomHttpForbiddenException(
              string module,
              IList<ErrorField> fields,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  errorCode: ErrorCodeType.FORBIDDEN,
                  errorMessage: ErrorCodeType.FORBIDDEN.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Validation constructor with rows only
        // ----------------------------------------------------------
        public CustomHttpForbiddenException(
              string module,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  rows: rows,
                  errorCode: ErrorCodeType.FORBIDDEN,
                  errorMessage: ErrorCodeType.FORBIDDEN.Description,
                  correlationId: correlationId)
        {

        }

        public CustomHttpForbiddenException(
              string module,
              IList<ErrorField> fields,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  rows: rows,
                  errorCode: ErrorCodeType.FORBIDDEN,
                  errorMessage: ErrorCodeType.FORBIDDEN.Description,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Backward compatibility constructor
        // ----------------------------------------------------------
        public CustomHttpForbiddenException(ErrorMessageResponse response)
            : base(response)
        {
        }
    }
}
