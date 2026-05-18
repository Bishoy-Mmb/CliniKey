using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, TokenResponse>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<TokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);
    }
}
