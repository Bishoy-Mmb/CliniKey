using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.ActivateClinic;

public sealed record ActivateClinicCommand(Guid ClinicId) : ICommand;
