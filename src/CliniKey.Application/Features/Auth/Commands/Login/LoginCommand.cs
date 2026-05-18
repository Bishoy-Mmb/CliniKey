using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;

namespace CliniKey.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<TokenResponse>;
