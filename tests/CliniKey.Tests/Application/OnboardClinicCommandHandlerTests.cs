using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Application.Features.Tenants.Commands.OnboardClinic;
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

public class OnboardClinicCommandHandlerTests
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantSchemaNameGenerator _tenantSchemaNameGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _clock;
    private readonly OnboardClinicCommandHandler _handler;

    public OnboardClinicCommandHandlerTests()
    {
        _clinicRepository = Substitute.For<IClinicRepository>();
        _tenantProvisioningService = Substitute.For<ITenantProvisioningService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _tenantSchemaNameGenerator = Substitute.For<ITenantSchemaNameGenerator>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));
        _tenantSchemaNameGenerator.Generate(Arg.Any<Guid>())
            .Returns(call => $"tenant_{call.Arg<Guid>().ToString("N")}");
        _handler = new OnboardClinicCommandHandler(
            _clinicRepository,
            _tenantProvisioningService,
            _currentUserService,
            _tenantSchemaNameGenerator,
            _unitOfWork,
            _clock);
    }

    [Fact]
    public async Task Handle_NoConflictAndProvisioningSucceeds_ReturnsProvisionedClinic()
    {
        var command = new OnboardClinicCommand("Cairo Dental Center", "01112345678", "15 Tahrir St");
        _clinicRepository.ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), null, Arg.Any<CancellationToken>()).Returns(false);
        _tenantProvisioningService
            .ProvisionAsync(Arg.Any<Clinic>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<string?>("202605230001_InitialTenantOperationalSchema"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(command.Name);
        result.Value.Phone.Should().Be(command.Phone);
        result.Value.SchemaName.Should().StartWith("tenant_");
        result.Value.SchemaName.Should().HaveLength(39);
        result.Value.ProvisioningStatus.Should().Be("Provisioned");
        result.Value.SchemaHealthStatus.Should().Be("Healthy");
        _clinicRepository.Received(1).Add(Arg.Any<Clinic>());
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicatePhone_ReturnsConflictWithoutProvisioning()
    {
        var command = new OnboardClinicCommand("Cairo Dental Center", "01112345678", "15 Tahrir St");
        _clinicRepository.ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.DuplicatePhone);
        _clinicRepository.DidNotReceive().Add(Arg.Any<Clinic>());
        await _tenantProvisioningService.DidNotReceive()
            .ProvisionAsync(Arg.Any<Clinic>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProvisioningFails_RemovesClinicAndReturnsFailure()
    {
        var command = new OnboardClinicCommand("Cairo Dental Center", "01112345678", "15 Tahrir St");
        Clinic? addedClinic = null;
        _clinicRepository.ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), null, Arg.Any<CancellationToken>()).Returns(false);
        _clinicRepository.When(x => x.Add(Arg.Any<Clinic>())).Do(call => addedClinic = call.Arg<Clinic>());
        _tenantProvisioningService
            .ProvisionAsync(Arg.Any<Clinic>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string?>(TenantErrors.ProvisioningFailed));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.ProvisioningFailed);
        addedClinic.Should().NotBeNull();
        _clinicRepository.Received(1).Remove(addedClinic!);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
