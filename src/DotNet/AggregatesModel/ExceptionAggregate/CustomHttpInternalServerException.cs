using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate
{
    public class CustomHttpInternalServerException : CustomHttpException
    {
        public CustomHttpInternalServerException(
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
        public CustomHttpInternalServerException(
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
        public CustomHttpInternalServerException(
              string module,
              string errorMessage,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: ErrorCodeType.UNEXPECTED,
                  errorMessage: errorMessage,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Validation constructor with fields only
        // ----------------------------------------------------------
        public CustomHttpInternalServerException(
              string module,
              IList<ErrorField> fields,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  errorCode: ErrorCodeType.UNEXPECTED,
                  errorMessage: ErrorCodeType.UNEXPECTED.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Validation constructor with rows only
        // ----------------------------------------------------------
        public CustomHttpInternalServerException(
              string module,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  rows: rows,
                  errorCode: ErrorCodeType.UNEXPECTED,
                  errorMessage: ErrorCodeType.UNEXPECTED.Description,
                  correlationId: correlationId)
        {

        }

        public CustomHttpInternalServerException(
              string module,
              IList<ErrorField> fields,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  rows: rows,
                  errorCode: ErrorCodeType.UNEXPECTED,
                  errorMessage: ErrorCodeType.UNEXPECTED.Description,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Backward compatibility constructor
        // ----------------------------------------------------------
        public CustomHttpInternalServerException(ErrorMessageResponse response)
            : base(response)
        {
        }
    }    
}
