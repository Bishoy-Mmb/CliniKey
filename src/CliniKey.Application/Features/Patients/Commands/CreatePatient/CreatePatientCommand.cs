using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Enums;

namespace CliniKey.Application.Features.Patients.Commands.CreatePatient;

public sealed record CreatePatientCommand(
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    Gender Gender,
    string? InsuranceDetails) : ICommand<Guid>;
