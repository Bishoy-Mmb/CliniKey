using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CliniKey.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace CliniKey.Infrastructure.Identity;

internal sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId => Guid.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : Guid.Empty;
    
    public string Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;
    
    public Guid TenantId => Guid.TryParse(User?.FindFirstValue("tenant_id"), out var id) ? id : Guid.Empty;
    
    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    
    public Guid? DentistId => Guid.TryParse(User?.FindFirstValue("dentist_id"), out var id) ? id : null;
    
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
