using CliniKey.Domain.Entities;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CliniKey.Tests.Infrastructure;

public class SharedSchemaMappingTests
{
    [Fact]
    public void AppDbContext_MapsCrossTenantTablesToSharedSchema()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=clinikey;Username=postgres")
            .Options;
        using var context = new AppDbContext(options);

        context.Model.FindEntityType(typeof(Clinic))!.GetSchema().Should().Be("shared");
        context.Model.FindEntityType(typeof(Dentist))!.GetSchema().Should().Be("shared");
        context.Model.FindEntityType(typeof(ClinicDentist))!.GetSchema().Should().Be("shared");
    }

    [Fact]
    public void AppDbContext_MapsCrossTenantTablesToConfiguredSharedSchema()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=clinikey;Username=postgres")
            .Options;
        using var context = new AppDbContext(
            options,
            Options.Create(new TenancyOptions { SharedSchema = "registry" }));

        context.Model.FindEntityType(typeof(Clinic))!.GetSchema().Should().Be("registry");
        context.Model.FindEntityType(typeof(Dentist))!.GetSchema().Should().Be("registry");
        context.Model.FindEntityType(typeof(ClinicDentist))!.GetSchema().Should().Be("registry");
    }

    [Fact]
    public void TenantSchemaNameGenerator_UsesConfiguredTenantSchemaPrefix()
    {
        var tenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var generator = new TenantSchemaNameGenerator(Options.Create(new TenancyOptions
        {
            TenantSchemaPrefix = "clinic_"
        }));

        var schemaName = generator.Generate(tenantId);

        schemaName.Should().Be("clinic_aaaaaaaabbbbccccddddeeeeeeeeeeee");
    }
}
