namespace CliniKey.Application.Abstractions.Identity;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    Guid TenantId { get; }
    string Role { get; }
    Guid? DentistId { get; }
    bool IsAuthenticated { get; }
}
