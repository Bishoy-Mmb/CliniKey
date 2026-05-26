using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.DeactivateTenant;

public sealed record DeactivateTenantCommand(
    Guid TenantId,
    string? Reason) : ICommand;
