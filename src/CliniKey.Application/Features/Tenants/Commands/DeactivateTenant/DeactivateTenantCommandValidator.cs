using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.DeactivateTenant;

public sealed class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    public DeactivateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");
    }
}
