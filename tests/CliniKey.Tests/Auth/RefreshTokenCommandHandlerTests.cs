using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.DTOs;
using CliniKey.Application.Features.Auth;
using CliniKey.Application.Features.Auth.Commands.RefreshToken;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CliniKey.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IAuthService _authServiceMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _authServiceMock = Substitute.For<IAuthService>();
        _handler = new RefreshTokenCommandHandler(_authServiceMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnTokenResponse_WhenRefreshTokenIsValid()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid_refresh_token");
        var expectedResponse = new TokenResponse("new_access_token", "new_refresh_token", DateTime.UtcNow.AddDays(7));

        _authServiceMock.RefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedResponse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid_refresh_token");

        _authServiceMock.RefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TokenResponse>(AuthErrors.InvalidRefreshToken));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
    }
}
