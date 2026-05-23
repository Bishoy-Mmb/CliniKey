using Microsoft.AspNetCore.Identity;

namespace CliniKey.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public Guid? DentistId { get; set; }
    public bool IsActive { get; set; } = true;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public void InitializeCreatedAt(DateTime createdAtUtc)
    {
        CreatedAtUtc = createdAtUtc;
    }
}
