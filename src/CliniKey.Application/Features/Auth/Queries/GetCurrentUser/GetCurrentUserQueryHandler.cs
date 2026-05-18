using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Auth.Queries.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var response = new CurrentUserResponse(
            _currentUserService.UserId,
            _currentUserService.Email,
            _currentUserService.Role,
            _currentUserService.TenantId,
            _currentUserService.DentistId);

        return Task.FromResult(Result.Success(response));
    }
}
