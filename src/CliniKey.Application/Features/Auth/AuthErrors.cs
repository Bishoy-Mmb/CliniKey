using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Failure(
        "Auth.InvalidCredentials",
        "Invalid email or password.");
        
    public static readonly Error DuplicateEmail = Error.Conflict(
        "Auth.DuplicateEmail",
        "Email is already registered.");
        
    public static readonly Error AccountDeactivated = Error.Failure(
        "Auth.AccountDeactivated",
        "Account has been deactivated.");
        
    public static readonly Error InvalidRefreshToken = Error.Failure(
        "Auth.InvalidRefreshToken",
        "Invalid refresh token.");
        
    public static readonly Error RefreshTokenExpired = Error.Failure(
        "Auth.RefreshTokenExpired",
        "Refresh token has expired.");
        
    public static readonly Error RefreshTokenRevoked = Error.Failure(
        "Auth.RefreshTokenRevoked",
        "Refresh token has been revoked.");
        
    public static readonly Error InvalidRole = Error.Validation(
        "Auth.InvalidRole",
        "Invalid role specified.");
        
    public static readonly Error WeakPassword = Error.Validation(
        "Auth.WeakPassword",
        "Password does not meet complexity requirements.");
}
