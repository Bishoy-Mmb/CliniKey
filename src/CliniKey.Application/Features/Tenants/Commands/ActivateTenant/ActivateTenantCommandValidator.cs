using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.ActivateTenant;

public sealed class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
    public ActivateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");
    }
}
