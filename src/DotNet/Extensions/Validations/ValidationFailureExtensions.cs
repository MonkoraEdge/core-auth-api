using FluentValidation.Results;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.DotNet.Extensions.Validations
{
    public static class ValidationFailureExtensions
    {
        public static List<ErrorField> ToErrorFields(this IEnumerable<ValidationFailure> failures)
        {
            var errorFields = failures.Select(error => new ErrorField(error.PropertyName, error.ErrorMessage)).ToList();

            return errorFields;
        }
    }
}
