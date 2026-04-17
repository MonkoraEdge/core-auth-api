using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using FluentValidation;

namespace MonkoraEdge.Core.Auth.Domain.Validations.AccountPermissionValidation;

public class TenantValidator : AbstractValidator<TenantCreateRequest>
{
    public TenantValidator()
    {
        RuleFor(m => m.TenantCode)
            .NotEmpty().WithMessage("TenantCode is required.")
            .MaximumLength(50).WithMessage("TenantCode cannot exceed 50 characters.")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("TenantCode must be uppercase alphanumeric (A-Z, 0-9, _, -).");

        RuleFor(m => m.TenantName)
            .NotNull().WithMessage("TenantName is required.");

        RuleFor(m => m.TenantName.EN)
            .NotEmpty().WithMessage("TenantName.EN is required.")
            .MaximumLength(200).WithMessage("TenantName.EN cannot exceed 200 characters.")
            .When(m => m.TenantName != null);
    }
}