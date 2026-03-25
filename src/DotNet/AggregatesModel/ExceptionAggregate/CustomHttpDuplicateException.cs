using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate
{
    public class CustomHttpDuplicateException : CustomHttpException
    {
        public CustomHttpDuplicateException(
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
        public CustomHttpDuplicateException(
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
        public CustomHttpDuplicateException(
              string module,
              string errorMessage,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: ErrorCodeType.DUPLICATE_TRANSACTION,
                  errorMessage: errorMessage,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Validation constructor with fields only
        // ----------------------------------------------------------
        public CustomHttpDuplicateException(
              string module,
              IList<ErrorField> fields,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  errorCode: ErrorCodeType.DUPLICATE_TRANSACTION,
                  errorMessage: ErrorCodeType.DUPLICATE_TRANSACTION.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Validation constructor with rows only
        // ----------------------------------------------------------
        public CustomHttpDuplicateException(
              string module,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  rows: rows,
                  errorCode: ErrorCodeType.DUPLICATE_TRANSACTION,
                  errorMessage: ErrorCodeType.DUPLICATE_TRANSACTION.Description,
                  correlationId: correlationId)
        {

        }

        public CustomHttpDuplicateException(
              string module,
              IList<ErrorField> fields,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  rows: rows,
                  errorCode: ErrorCodeType.DUPLICATE_TRANSACTION,
                  errorMessage: ErrorCodeType.DUPLICATE_TRANSACTION.Description,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Backward compatibility constructor
        // ----------------------------------------------------------
        public CustomHttpDuplicateException(ErrorMessageResponse response)
            : base(response)
        {
        }
    }    
}
