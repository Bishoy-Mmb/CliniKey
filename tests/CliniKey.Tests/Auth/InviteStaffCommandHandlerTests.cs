using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Features.Auth.Commands.InviteStaff;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CliniKey.Tests.Auth;

public class InviteStaffCommandHandlerTests
{
    private readonly IAuthService _authServiceMock;
    private readonly InviteStaffCommandHandler _handler;

    public InviteStaffCommandHandlerTests()
    {
        _authServiceMock = Substitute.For<IAuthService>();
        _handler = new InviteStaffCommandHandler(_authServiceMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenInvitationIsSuccessful()
    {
        // Arrange
        var command = new InviteStaffCommand("dentist@clinic.com", "P@ssw0rd123!", "Dr. Smith", "Dentist", "General", "LIC-1234");
        var expectedUserId = Guid.NewGuid();

        _authServiceMock.InviteStaffAsync(
            command.Email, command.Password, command.FullName, command.Role, command.Specialization, command.LicenseNumber, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedUserId));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedUserId);
    }
}
