using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.OnboardTenant;

public sealed record OnboardTenantCommand(
    string Name,
    string Phone,
    string Address) : ICommand<OnboardTenantResponse>;
