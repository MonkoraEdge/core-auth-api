using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate
{
    public class CustomHttpNotFoundException : CustomHttpException
    {
        public CustomHttpNotFoundException(
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
        public CustomHttpNotFoundException(
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
        public CustomHttpNotFoundException(
              string module,
              string errorMessage,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: ErrorCodeType.NOT_FOUND,
                  errorMessage: errorMessage,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Validation constructor with fields only
        // ----------------------------------------------------------
        public CustomHttpNotFoundException(
              string module,
              IList<ErrorField> fields,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  errorCode: ErrorCodeType.NOT_FOUND,
                  errorMessage: ErrorCodeType.NOT_FOUND.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Validation constructor with rows only
        // ----------------------------------------------------------
        public CustomHttpNotFoundException(
              string module,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  rows: rows,
                  errorCode: ErrorCodeType.NOT_FOUND,
                  errorMessage: ErrorCodeType.NOT_FOUND.Description,
                  correlationId: correlationId)
        {

        }

        public CustomHttpNotFoundException(
              string module,
              IList<ErrorField> fields,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  rows: rows,
                  errorCode: ErrorCodeType.NOT_FOUND,
                  errorMessage: ErrorCodeType.NOT_FOUND.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Backward compatibility constructor
        // ----------------------------------------------------------
        public CustomHttpNotFoundException(ErrorMessageResponse response)
            : base(response)
        {
        }
    }
}
