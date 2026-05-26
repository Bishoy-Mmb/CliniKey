using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;

public sealed class MigrateTenantSchemasCommandValidator : AbstractValidator<MigrateTenantSchemasCommand>
{
    public MigrateTenantSchemasCommandValidator()
    {
        RuleForEach(x => x.TenantIds)
            .NotEmpty().When(x => x.TenantIds is not null);
    }
}
