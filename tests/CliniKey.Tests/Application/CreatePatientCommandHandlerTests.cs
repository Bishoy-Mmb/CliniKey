using CliniKey.Application.Features.Patients.Commands.CreatePatient;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CliniKey.Tests.Application;

public class CreatePatientCommandHandlerTests
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreatePatientCommandHandler _handler;

    public CreatePatientCommandHandlerTests()
    {
        _patientRepository = Substitute.For<IPatientRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreatePatientCommandHandler(_patientRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoConflict_Succeeds()
    {
        // Arrange
        var command = new CreatePatientCommand(
            "Ahmed",
            "Hassan",
            "01012345678",
            new DateOnly(1990, 1, 1),
            Gender.Male,
            null);

        _patientRepository
            .ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        _patientRepository.Received(1).Add(Arg.Is<Patient>(p => 
            p.Name.FirstName == "Ahmed" && 
            p.Phone.Value == "01012345678"));
            
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicatePhone_ReturnsConflict()
    {
        // Arrange
        var command = new CreatePatientCommand(
            "Ahmed",
            "Hassan",
            "01012345678",
            new DateOnly(1990, 1, 1),
            Gender.Male,
            null);

        _patientRepository
            .ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PatientErrors.DuplicatePhone);

        _patientRepository.DidNotReceive().Add(Arg.Any<Patient>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
