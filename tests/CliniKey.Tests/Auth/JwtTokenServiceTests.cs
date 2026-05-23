using System.IdentityModel.Tokens.Jwt;
using CliniKey.Application.Constants;
using CliniKey.Infrastructure.Identity;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

using Microsoft.Extensions.Time.Testing;

namespace CliniKey.Tests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly FakeTimeProvider _clock;
    private readonly DateTimeOffset _fixedTime = new(2026, 5, 21, 10, 0, 0, TimeSpan.Zero);
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "SuperSecretKeyForTestingTokensWhichIsAtLeast32BytesLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        _clock = new FakeTimeProvider(_fixedTime);
        var options = Options.Create(_jwtSettings);
        _jwtTokenService = new JwtTokenService(options, _clock);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt_WithCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@clinic.com";
        var role = Roles.ClinicAdmin;
        var tenantId = Guid.NewGuid();
        Guid? dentistId = Guid.NewGuid();

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, email, role, tenantId, dentistId);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
        jwtToken.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "dentist_id" && c.Value == dentistId.ToString());
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrWhiteSpace();
        
        // Should be valid Base64
        Action act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
    }
}
