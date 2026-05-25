using CliniKey.API.Controllers;
using CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;
using CliniKey.Application.Features.Tenants.Commands.UpdateClinicContact;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Application.Features.Tenants.Queries.GetClinicById;
using CliniKey.Application.Features.Tenants.Queries.ListClinics;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CliniKey.Tests.API;

public class TenantsControllerTests
{
    private readonly ISender _sender;
    private readonly TenantsController _controller;

    public TenantsControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _controller = new TenantsController(_sender);
    }

    [Fact]
    public async Task GetClinicById_ReturnsOkWithClinicDetails()
    {
        var clinicId = Guid.NewGuid();
        var response = CreateClinicResponse(clinicId);
        _sender.Send(Arg.Any<GetClinicByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.GetClinicById(clinicId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task ListClinics_ReturnsOkWithPagedClinics()
    {
        var response = new ClinicListResponse(
            [new ClinicListItemResponse(Guid.NewGuid(), Guid.NewGuid(), "Cairo Dental Center", "01112345678", "15 Tahrir St", "tenant_ab12cd34", "Active", "Active", "Provisioned", "Healthy", null)],
            1,
            50,
            1);
        _sender.Send(Arg.Any<ListClinicsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.ListClinics(null, null, 1, 50, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task UpdateClinicContact_Success_ReturnsNoContentAndSendsRouteId()
    {
        var clinicId = Guid.NewGuid();
        UpdateClinicContactCommand? sentCommand = null;
        _sender
            .Send(Arg.Do<UpdateClinicContactCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpdateClinicContact(
            clinicId,
            new TenantsController.UpdateClinicContactRequest("01198765432", "22 Nile Corniche"),
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        sentCommand.Should().NotBeNull();
        sentCommand!.ClinicId.Should().Be(clinicId);
        sentCommand.Phone.Should().Be("01198765432");
        sentCommand.Address.Should().Be("22 Nile Corniche");
    }

    private static ClinicResponse CreateClinicResponse(Guid clinicId)
    {
        return new ClinicResponse(
            Guid.NewGuid(),
            clinicId,
            "Cairo Dental Center",
            "01112345678",
            "15 Tahrir St",
            "tenant_ab12cd34",
            "Active",
            "Active",
            "Provisioned",
            "Healthy",
            "202605230001_InitialTenantOperationalSchema",
            null,
            null,
            null,
            null,
            null,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc),
            null);
    }
}

public class TenantMigrationsControllerTests
{
    private readonly ISender _sender;
    private readonly TenantMigrationsController _controller;

    public TenantMigrationsControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _controller = new TenantMigrationsController(_sender);
    }

    [Fact]
    public async Task Apply_WhenAnyTenantMigrationFails_ReturnsServerErrorWithPartialResults()
    {
        var response = new MigrateTenantSchemasResponse(
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 23, 10, 0, 5, DateTimeKind.Utc),
            "202605230001_InitialTenantOperationalSchema",
            [
                new TenantMigrationResultResponse(
                    Guid.NewGuid(),
                    "tenant_failed",
                    "Failed",
                    "202605180001_PreviousMigration",
                    null,
                    "Migration failed")
            ]);
        _sender.Send(Arg.Any<MigrateTenantSchemasCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.Apply(new MigrateTenantSchemasCommand(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().Be(response);
    }
}
