namespace CliniKey.Infrastructure.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public void Initialize(DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Revoke(DateTime revokedAtUtc)
    {
        RevokedAtUtc = revokedAtUtc;
    }
}
