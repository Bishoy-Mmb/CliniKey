using FluentValidation;

namespace CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;

public sealed class MigrateTenantSchemasCommandValidator : AbstractValidator<MigrateTenantSchemasCommand>
{
    public MigrateTenantSchemasCommandValidator()
    {
        RuleForEach(x => x.ClinicIds)
            .NotEmpty().When(x => x.ClinicIds is not null);
    }
}
