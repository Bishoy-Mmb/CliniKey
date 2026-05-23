using CliniKey.Domain.Entities;
using CliniKey.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

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
}
