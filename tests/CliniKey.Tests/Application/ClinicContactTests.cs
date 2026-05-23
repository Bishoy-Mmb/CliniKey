using CliniKey.Application.Features.Tenants.Commands.UpdateClinicContact;
using CliniKey.Application.Features.Tenants.Queries.GetClinicById;
using CliniKey.Application.Features.Tenants.Queries.ListClinics;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CliniKey.Tests.Application;

public class ClinicContactTests
{
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _clock;

    public ClinicContactTests()
    {
        _clinicRepository = Substitute.For<IClinicRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetClinicById_ExistingClinic_ReturnsDetails()
    {
        var clinic = CreateClinic("Cairo Dental Center", "01112345678");
        _clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var handler = new GetClinicByIdQueryHandler(_clinicRepository);

        var result = await handler.Handle(new GetClinicByIdQuery(clinic.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClinicId.Should().Be(clinic.Id);
        result.Value.Phone.Should().Be("01112345678");
        result.Value.Address.Should().Be("15 Tahrir St");
    }

    [Fact]
    public async Task ListClinics_ReturnsPagedClinicsAndTotalCount()
    {
        var clinic = CreateClinic("Cairo Dental Center", "01112345678");
        _clinicRepository
            .ListAsync(ClinicStatus.Active, TenantSchemaHealthStatus.Healthy, 1, 50, Arg.Any<CancellationToken>())
            .Returns([clinic]);
        _clinicRepository
            .CountAsync(ClinicStatus.Active, TenantSchemaHealthStatus.Healthy, Arg.Any<CancellationToken>())
            .Returns(1);
        var handler = new ListClinicsQueryHandler(_clinicRepository);

        var result = await handler.Handle(
            new ListClinicsQuery(ClinicStatus.Active, TenantSchemaHealthStatus.Healthy),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(i => i.ClinicId == clinic.Id);
    }

    [Fact]
    public async Task UpdateClinicContact_ValidInput_UpdatesAndSaves()
    {
        var clinic = CreateClinic("Cairo Dental Center", "01112345678");
        _clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _clinicRepository
            .ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), clinic.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new UpdateClinicContactCommandHandler(_clinicRepository, _unitOfWork);

        var result = await handler.Handle(
            new UpdateClinicContactCommand(clinic.Id, "01198765432", "22 Nile Corniche"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clinic.Phone.Value.Should().Be("01198765432");
        clinic.Address.Should().Be("22 Nile Corniche");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateClinicContact_DuplicatePhone_ReturnsConflictWithoutSaving()
    {
        var clinic = CreateClinic("Cairo Dental Center", "01112345678");
        _clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _clinicRepository
            .ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), clinic.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new UpdateClinicContactCommandHandler(_clinicRepository, _unitOfWork);

        var result = await handler.Handle(
            new UpdateClinicContactCommand(clinic.Id, "01198765432", "22 Nile Corniche"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.DuplicatePhone);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Clinic CreateClinic(string name, string phone)
    {
        var clinic = Clinic.Create(name, phone, "15 Tahrir St", _clock).Value;
        clinic.MarkProvisioned("202605230001_InitialTenantOperationalSchema");
        clinic.ClearDomainEvents();
        return clinic;
    }
}
