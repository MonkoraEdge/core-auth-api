using MonkoraEdge.Core.Auth.Domain.AggregatesModel.TenantAggregate;
using FluentValidation;

namespace MonkoraEdge.Core.Auth.Domain.Validations.AccountPermissionValidation;

public class TenantValidator : AbstractValidator<TenantCreateRequest>
{
    public TenantValidator()
    {
        RuleFor(m => m.Status).NotNull()
            .NotEmpty().WithMessage($"{nameof(TenantCreateRequest.Status)} is required.");
               
    }
}