using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Features.Auth.Commands.Register;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CliniKey.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly IAuthService _authServiceMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _authServiceMock = Substitute.For<IAuthService>();
        _handler = new RegisterCommandHandler(_authServiceMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var command = new RegisterCommand("admin@clinic.com", "P@ssw0rd123!", "Admin User", Guid.NewGuid());
        var expectedUserId = Guid.NewGuid();

        _authServiceMock.RegisterAsync(command.Email, command.Password, command.FullName, command.ClinicId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedUserId));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedUserId);
    }
}
