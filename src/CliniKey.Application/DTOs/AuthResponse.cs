namespace CliniKey.Application.DTOs;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string Role);
