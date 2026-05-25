namespace CliniKey.Application.Abstractions.Tenancy;

public interface ITenantSchemaNameGenerator
{
    string Generate(Guid tenantId);
}
