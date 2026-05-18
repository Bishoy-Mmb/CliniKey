using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Auth.Commands.InviteStaff;

public sealed record InviteStaffCommand(
    string Email,
    string Password,
    string FullName,
    string Role,
    string? Specialization,
    string? LicenseNumber) : ICommand<Guid>;
