using CliniKey.Application.Abstractions.Identity;
using CliniKey.SharedKernel.Primitives;
using Microsoft.AspNetCore.Identity;
using CliniKey.Domain.Repositories;
using CliniKey.Application.DTOs;
using CliniKey.Application.Constants;
using CliniKey.Application.Features.Auth;
using CliniKey.Application.Features.Auth.Queries.GetUserById;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;

using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using Microsoft.Extensions.Options;

namespace CliniKey.Infrastructure.Identity;

internal sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AuthDbContext _authDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDentistRepository _dentistRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly TimeProvider _clock;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IClinicRepository clinicRepository,
        ITenantRepository tenantRepository,
        IJwtTokenService jwtTokenService,
        AuthDbContext authDbContext,
        ICurrentUserService currentUserService,
        IDentistRepository dentistRepository,
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        TimeProvider clock)
    {
        _userManager = userManager;
        _clinicRepository = clinicRepository;
        _tenantRepository = tenantRepository;
        _jwtTokenService = jwtTokenService;
        _authDbContext = authDbContext;
        _currentUserService = currentUserService;
        _dentistRepository = dentistRepository;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _clock = clock;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public async Task<Result<Guid>> RegisterAsync(string email, string password, string fullName, Guid clinicId, CancellationToken cancellationToken = default)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Clinic.NotFound", $"The clinic with ID '{clinicId}' was not found."));
        }

        var tenant = await _tenantRepository.GetByIdAsync(clinic.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<Guid>(TenantErrors.NotFound);
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return Result.Failure<Guid>(AuthErrors.DuplicateEmail);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            TenantId = tenant.Id,
            IsActive = true
        };
        user.InitializeCreatedAt(_clock.GetUtcNow().UtcDateTime);

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return Result.Failure<Guid>(Error.Failure("Auth.RegistrationFailed", result.Errors.First().Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, Roles.ClinicAdmin);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Result.Failure<Guid>(
                AuthErrors.RoleAssignmentFailed(
                    string.Join(", ", roleResult.Errors.Select(e => e.Description))));
        }

        return Result.Success(user.Id);
    }
    public async Task<Result<TokenResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure<TokenResponse>(AuthErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<TokenResponse>(AuthErrors.AccountDeactivated);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return Result.Failure<TokenResponse>(AuthErrors.InvalidCredentials);
        }

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return Result.Failure<TokenResponse>(AuthErrors.ClinicDeactivated);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? string.Empty;

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, user.TenantId, user.DentistId);
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var hashedTokenString = HashToken(refreshTokenString);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = hashedTokenString,
            UserId = user.Id,
            FamilyId = Guid.NewGuid()
        };
        refreshToken.Initialize(_clock.GetUtcNow().UtcDateTime, _clock.GetUtcNow().UtcDateTime.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        _authDbContext.RefreshTokens.Add(refreshToken);
        await _authDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new TokenResponse(accessToken, refreshTokenString, refreshToken.ExpiresAtUtc));
    }
    public async Task<Result<Guid>> InviteStaffAsync(string email, string password, string fullName, string role, string? specialization, string? licenseNumber, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<Guid>(Error.Failure("Auth.Unauthorized", "No active tenant found."));
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return Result.Failure<Guid>(AuthErrors.DuplicateEmail);
        }

        Guid? newDentistId = null;

        if (role == Roles.Dentist)
        {
            var dentistResult = CliniKey.Domain.Entities.Dentist.Create(fullName, specialization!, licenseNumber!, _clock);
            if (dentistResult.IsFailure)
            {
                return Result.Failure<Guid>(dentistResult.Error);
            }

            var dentist = dentistResult.Value;
            _dentistRepository.Add(dentist);
            
            var clinic = await _clinicRepository.GetPrimaryByTenantIdAsync(tenantId, cancellationToken);
            if (clinic is not null)
            {
                clinic.AddDentist(dentist.Id);
            }
            newDentistId = dentist.Id;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            TenantId = tenantId,
            DentistId = newDentistId,
            IsActive = true
        };
        user.InitializeCreatedAt(_clock.GetUtcNow().UtcDateTime);

        using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return Result.Failure<Guid>(Error.Failure("Auth.InvitationFailed", result.Errors.First().Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Result.Failure<Guid>(
                AuthErrors.RoleAssignmentFailed(
                    string.Join(", ", roleResult.Errors.Select(e => e.Description))));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        transaction.Complete();

        return Result.Success(user.Id);
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hashedTokenString = HashToken(refreshToken);

        var tokenEntity = await _authDbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hashedTokenString, cancellationToken);

        if (tokenEntity is null)
        {
            return Result.Failure<TokenResponse>(AuthErrors.InvalidRefreshToken);
        }

        if (tokenEntity.ExpiresAtUtc < _clock.GetUtcNow().UtcDateTime)
        {
            return Result.Failure<TokenResponse>(AuthErrors.RefreshTokenExpired);
        }

        if (tokenEntity.RevokedAtUtc is not null)
        {
            var familyTokens = await _authDbContext.RefreshTokens
                .Where(rt => rt.FamilyId == tokenEntity.FamilyId && rt.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
                
            foreach (var t in familyTokens)
            {
                t.Revoke(_clock.GetUtcNow().UtcDateTime);
            }
            await _authDbContext.SaveChangesAsync(cancellationToken);

            return Result.Failure<TokenResponse>(AuthErrors.RefreshTokenRevoked);
        }

        var user = await _userManager.FindByIdAsync(tokenEntity.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<TokenResponse>(AuthErrors.AccountDeactivated);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? string.Empty;

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, user.TenantId, user.DentistId);
        var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var newHashedString = HashToken(newRefreshTokenString);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = newHashedString,
            UserId = user.Id,
            FamilyId = tokenEntity.FamilyId
        };
        newRefreshToken.Initialize(_clock.GetUtcNow().UtcDateTime, _clock.GetUtcNow().UtcDateTime.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        tokenEntity.Revoke(_clock.GetUtcNow().UtcDateTime);
        tokenEntity.ReplacedByTokenId = newRefreshToken.Id;

        _authDbContext.RefreshTokens.Add(newRefreshToken);
        await _authDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new TokenResponse(newAccessToken, newRefreshTokenString, newRefreshToken.ExpiresAtUtc));
    }

    public async Task<Result<UserByIdResponse>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.TenantId != _currentUserService.TenantId)
        {
            return Result.Failure<UserByIdResponse>(AuthErrors.UserNotFound(userId));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = new UserByIdResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            roles.FirstOrDefault() ?? string.Empty,
            user.TenantId,
            user.DentistId,
            user.IsActive);

        return response;
    }
}
