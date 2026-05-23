using CliniKey.Application.DTOs;
using CliniKey.Application.Features.Auth.Queries.GetUserById;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Abstractions.Identity;

public interface IAuthService
{
    Task<Result<Guid>> RegisterAsync(string email, string password, string fullName, Guid clinicId, CancellationToken cancellationToken = default);
    
    Task<Result<TokenResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    
    Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    Task<Result<Guid>> InviteStaffAsync(string email, string password, string fullName, string role, string? specialization, string? licenseNumber, CancellationToken cancellationToken = default);

    Task<Result<UserByIdResponse>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
