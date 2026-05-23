using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Auth.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserByIdResponse>;

public sealed record UserByIdResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid TenantId,
    Guid? DentistId,
    bool IsActive);
