using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using CliniKey.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace CliniKey.Tests.Auth;

public class CurrentUserServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessorMock;
    private readonly CurrentUserService _currentUserService;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = Substitute.For<IHttpContextAccessor>();
        _currentUserService = new CurrentUserService(_httpContextAccessorMock);
    }

    [Fact]
    public void Properties_ShouldReturnValues_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@clinic.com";
        var tenantId = Guid.NewGuid();
        var dentistId = Guid.NewGuid();
        var role = "Dentist";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("tenant_id", tenantId.ToString()),
            new("dentist_id", dentistId.ToString()),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.HttpContext.Returns(httpContext);

        // Act & Assert
        _currentUserService.UserId.Should().Be(userId);
        _currentUserService.Email.Should().Be(email);
        _currentUserService.TenantId.Should().Be(tenantId);
        _currentUserService.DentistId.Should().Be(dentistId);
        _currentUserService.Role.Should().Be(role);
        _currentUserService.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void Properties_ShouldReturnDefaults_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.HttpContext.Returns(httpContext);

        // Act & Assert
        _currentUserService.UserId.Should().Be(Guid.Empty);
        _currentUserService.Email.Should().BeEmpty();
        _currentUserService.TenantId.Should().Be(Guid.Empty);
        _currentUserService.DentistId.Should().BeNull();
        _currentUserService.Role.Should().BeEmpty();
        _currentUserService.IsAuthenticated.Should().BeFalse();
    }
}
