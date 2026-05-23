using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.UpdateClinicContact;

public sealed record UpdateClinicContactCommand(
    Guid ClinicId,
    string Phone,
    string Address) : ICommand;
