using CliniKey.Application.Abstractions.Tenancy;
using Microsoft.Extensions.Options;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class TenantSchemaNameGenerator : ITenantSchemaNameGenerator
{
    private readonly TenancyOptions _options;

    public TenantSchemaNameGenerator(IOptions<TenancyOptions> options)
    {
        _options = options.Value;
    }

    public string Generate(Guid tenantId)
    {
        return $"{_options.TenantSchemaPrefix}{tenantId:N}";
    }
}
