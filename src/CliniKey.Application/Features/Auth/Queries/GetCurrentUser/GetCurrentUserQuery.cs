using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery() : IQuery<CurrentUserResponse>;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string Role,
    Guid TenantId,
    Guid? DentistId);
