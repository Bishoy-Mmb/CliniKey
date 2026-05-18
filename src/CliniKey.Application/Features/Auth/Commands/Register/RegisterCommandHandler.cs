using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Auth.Commands.Register;

internal sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, Guid>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RegisterAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.ClinicId,
            cancellationToken);
    }
}
