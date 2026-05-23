using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.DeactivateClinic;

public sealed record DeactivateClinicCommand(
    Guid ClinicId,
    string? Reason) : ICommand;
