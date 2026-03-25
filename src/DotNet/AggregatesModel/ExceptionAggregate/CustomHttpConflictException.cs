using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate
{
    public class CustomHttpConflictException : CustomHttpException
    {
        public CustomHttpConflictException(
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
        public CustomHttpConflictException(
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
        public CustomHttpConflictException(
              string module,
              string errorMessage,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: ErrorCodeType.OPERATION_CONFLICT,
                  errorMessage: errorMessage,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Validation constructor with fields only
        // ----------------------------------------------------------
        public CustomHttpConflictException(
              string module,
              IList<ErrorField> fields,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  errorCode: ErrorCodeType.OPERATION_CONFLICT,
                  errorMessage: ErrorCodeType.OPERATION_CONFLICT.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Validation constructor with rows only
        // ----------------------------------------------------------
        public CustomHttpConflictException(
              string module,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  rows: rows,
                  errorCode: ErrorCodeType.OPERATION_CONFLICT,
                  errorMessage: ErrorCodeType.OPERATION_CONFLICT.Description,
                  correlationId: correlationId)
        {

        }

        public CustomHttpConflictException(
              string module,
              IList<ErrorField> fields,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  rows: rows,
                  errorCode: ErrorCodeType.OPERATION_CONFLICT,
                  errorMessage: ErrorCodeType.OPERATION_CONFLICT.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Backward compatibility constructor
        // ----------------------------------------------------------
        public CustomHttpConflictException(ErrorMessageResponse response)
            : base(response)
        {
        }
    }
}
