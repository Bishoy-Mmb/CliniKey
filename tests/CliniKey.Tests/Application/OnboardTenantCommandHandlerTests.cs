using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Application.Features.Tenants.Commands.OnboardTenant;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CliniKey.Tests.Application;

public class OnboardTenantCommandHandlerTests
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantSchemaNameGenerator _tenantSchemaNameGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _clock;
    private readonly OnboardTenantCommandHandler _handler;

    public OnboardTenantCommandHandlerTests()
    {
        _clinicRepository = Substitute.For<IClinicRepository>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantProvisioningService = Substitute.For<ITenantProvisioningService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _tenantSchemaNameGenerator = Substitute.For<ITenantSchemaNameGenerator>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));
        _tenantSchemaNameGenerator.Generate(Arg.Any<Guid>())
            .Returns(call => $"tenant_{call.Arg<Guid>().ToString("N")}");
        _handler = new OnboardTenantCommandHandler(
            _clinicRepository,
            _tenantRepository,
            _tenantProvisioningService,
            _currentUserService,
            _tenantSchemaNameGenerator,
            _unitOfWork,
            _clock);
    }

    [Fact]
    public async Task Handle_NoConflictAndProvisioningSucceeds_ReturnsProvisionedClinic()
    {
        var command = new OnboardTenantCommand("Cairo Dental Center", "01112345678", "15 Tahrir St");
        _clinicRepository.ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), null, Arg.Any<CancellationToken>()).Returns(false);
        _tenantProvisioningService
            .ProvisionAsync(Arg.Any<Tenant>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<string?>("202605230001_InitialTenantOperationalSchema"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(command.Name);
        result.Value.Phone.Should().Be(command.Phone);
        result.Value.SchemaName.Should().StartWith("tenant_");
        result.Value.SchemaName.Should().HaveLength(39);
        result.Value.Status.Should().Be("Active");
        result.Value.TenantStatus.Should().Be("Active");
        result.Value.ProvisioningStatus.Should().Be("Provisioned");
        result.Value.SchemaHealthStatus.Should().Be("Healthy");
        result.Value.TenantId.Should().NotBe(Guid.Empty);
        result.Value.ClinicId.Should().NotBe(Guid.Empty);
        result.Value.TenantId.Should().NotBe(result.Value.ClinicId);
        _tenantRepository.Received(1).Add(Arg.Any<Tenant>());
        _clinicRepository.Received(1).Add(Arg.Any<Clinic>());
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicatePhone_ReturnsConflictWithoutProvisioning()
    {
        var command = new OnboardTenantCommand("Cairo Dental Center", "01112345678", "15 Tahrir St");
        _clinicRepository.ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.DuplicatePhone);
        _clinicRepository.DidNotReceive().Add(Arg.Any<Clinic>());
        _tenantRepository.DidNotReceive().Add(Arg.Any<Tenant>());
        await _tenantProvisioningService.DidNotReceive()
            .ProvisionAsync(Arg.Any<Tenant>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProvisioningFails_RemovesClinicAndReturnsFailure()
    {
        var command = new OnboardTenantCommand("Cairo Dental Center", "01112345678", "15 Tahrir St");
        Clinic? addedClinic = null;
        Tenant? addedTenant = null;
        _clinicRepository.ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), null, Arg.Any<CancellationToken>()).Returns(false);
        _clinicRepository.When(x => x.Add(Arg.Any<Clinic>())).Do(call => addedClinic = call.Arg<Clinic>());
        _tenantRepository.When(x => x.Add(Arg.Any<Tenant>())).Do(call => addedTenant = call.Arg<Tenant>());
        _tenantProvisioningService
            .ProvisionAsync(Arg.Any<Tenant>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string?>(TenantErrors.ProvisioningFailed));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.ProvisioningFailed);
        addedClinic.Should().NotBeNull();
        addedTenant.Should().NotBeNull();
        _clinicRepository.Received(1).Remove(addedClinic!);
        _tenantRepository.Received(1).Remove(addedTenant!);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
