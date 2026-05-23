using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.DTOs;
using CliniKey.Application.Features.Auth;
using CliniKey.Application.Features.Auth.Commands.Login;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace CliniKey.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly IAuthService _authServiceMock;
    private readonly LoginCommandHandler _handler;
    private readonly FakeTimeProvider _clock;
    private readonly DateTimeOffset _fixedTime = new(2026, 5, 21, 10, 0, 0, TimeSpan.Zero);

    public LoginCommandHandlerTests()
    {
        _authServiceMock = Substitute.For<IAuthService>();
        _clock = new FakeTimeProvider(_fixedTime);
        _handler = new LoginCommandHandler(_authServiceMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnTokenResponse_WhenLoginIsSuccessful()
    {
        // Arrange
        var command = new LoginCommand("user@clinic.com", "Password123!");
        var expectedResponse = new TokenResponse(
            "access_token",
            "refresh_token",
            _clock.GetUtcNow().UtcDateTime.AddDays(7));

        _authServiceMock.LoginAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedResponse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCredentialsAreInvalid()
    {
        // Arrange
        var command = new LoginCommand("user@clinic.com", "WrongPassword!");

        _authServiceMock.LoginAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TokenResponse>(AuthErrors.InvalidCredentials));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
    }
}
