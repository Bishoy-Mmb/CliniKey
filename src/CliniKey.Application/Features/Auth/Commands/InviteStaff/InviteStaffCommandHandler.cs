using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Auth.Commands.InviteStaff;

internal sealed class InviteStaffCommandHandler : ICommandHandler<InviteStaffCommand, Guid>
{
    private readonly IAuthService _authService;

    public InviteStaffCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<Guid>> Handle(InviteStaffCommand request, CancellationToken cancellationToken)
    {
        return await _authService.InviteStaffAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.Role,
            request.Specialization,
            request.LicenseNumber,
            cancellationToken);
    }
}
