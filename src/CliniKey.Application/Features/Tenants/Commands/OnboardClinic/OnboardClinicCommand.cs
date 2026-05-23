using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.OnboardClinic;

public sealed record OnboardClinicCommand(
    string Name,
    string Phone,
    string Address) : ICommand<OnboardClinicResponse>;
