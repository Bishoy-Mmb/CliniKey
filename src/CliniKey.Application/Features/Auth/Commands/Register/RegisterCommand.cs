using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    Guid ClinicId) : ICommand<Guid>;
