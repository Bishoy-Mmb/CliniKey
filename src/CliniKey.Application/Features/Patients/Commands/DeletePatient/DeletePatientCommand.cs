using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Patients.Commands.DeletePatient;

public sealed record DeletePatientCommand(Guid PatientId) : ICommand;
