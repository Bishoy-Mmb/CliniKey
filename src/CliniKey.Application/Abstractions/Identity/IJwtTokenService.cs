

namespace CliniKey.Application.Abstractions.Identity;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role, Guid tenantId, Guid? dentistId);
    
    string GenerateRefreshToken();
}
