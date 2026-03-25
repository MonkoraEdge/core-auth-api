using FluentValidation.Results;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate
{
    public class CustomHttpValidationException : CustomHttpException
    {
        public IEnumerable<ValidationFailure> ValidationErrors { get; }
        public CustomHttpValidationException(
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
        public CustomHttpValidationException(
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
        public CustomHttpValidationException(
              string module,
              string errorMessage,
              string correlationId = null)
              : base(
                  module: module,
                  errorCode: ErrorCodeType.VALIDATION_FAILED,
                  errorMessage: errorMessage,
                  correlationId: correlationId)
        {

        }


        // ----------------------------------------------------------
        // Validation constructor with fields only
        // ----------------------------------------------------------
        public CustomHttpValidationException(
              string module,
              IList<ErrorField> fields,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  errorCode: ErrorCodeType.VALIDATION_FAILED,
                  errorMessage: ErrorCodeType.VALIDATION_FAILED.Description,
                  correlationId: correlationId)
        {

        }

        // ----------------------------------------------------------
        // Validation constructor with rows only
        // ----------------------------------------------------------
        public CustomHttpValidationException(
              string module,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  rows: rows,
                  errorCode: ErrorCodeType.VALIDATION_FAILED,
                  errorMessage: ErrorCodeType.VALIDATION_FAILED.Description,
                  correlationId: correlationId)
        {

        }

        public CustomHttpValidationException(
              string module,
              IList<ErrorField> fields,
              IList<ErrorRow> rows,
              string correlationId = null)
              : base(
                  module: module,
                  fields: fields,
                  rows: rows,
                  errorCode: ErrorCodeType.VALIDATION_FAILED,
                  errorMessage: ErrorCodeType.VALIDATION_FAILED.Description,
                  correlationId: correlationId)
        {

        }


        public CustomHttpValidationException(string module, IEnumerable<ValidationFailure> failures)
            : base(module, ErrorCodeType.VALIDATION_FAILED, "One or more validation erros have occurred.")
        {
            ValidationErrors = failures;
        }

        // ----------------------------------------------------------
        // Backward compatibility constructor
        // ----------------------------------------------------------
        public CustomHttpValidationException(ErrorMessageResponse response)
            : base(response)
        {
        }
    }
}
