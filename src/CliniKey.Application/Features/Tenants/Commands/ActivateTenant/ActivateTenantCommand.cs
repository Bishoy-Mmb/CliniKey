using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.ActivateTenant;

public sealed record ActivateTenantCommand(Guid TenantId) : ICommand;
