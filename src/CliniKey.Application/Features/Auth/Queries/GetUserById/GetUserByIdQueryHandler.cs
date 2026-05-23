using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Auth.Queries.GetUserById;

internal sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserByIdResponse>
{
    private readonly IAuthService _authService;

    public GetUserByIdQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<UserByIdResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _authService.GetUserByIdAsync(request.UserId, cancellationToken);
    }
}
