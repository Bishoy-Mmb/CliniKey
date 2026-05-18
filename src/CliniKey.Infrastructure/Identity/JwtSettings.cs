namespace CliniKey.Infrastructure.Identity;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public int AccessTokenExpirationMinutes { get; init; }
    public int RefreshTokenExpirationDays { get; init; }
}
