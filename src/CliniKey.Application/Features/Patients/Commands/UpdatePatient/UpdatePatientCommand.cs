using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Patients.Commands.UpdatePatient;

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string PhoneNumber,
    string? InsuranceDetails) : ICommand;
