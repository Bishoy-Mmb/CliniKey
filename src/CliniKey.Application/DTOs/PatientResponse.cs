using CliniKey.Domain.Enums;

namespace CliniKey.Application.DTOs;

public sealed record PatientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    Gender Gender,
    string? InsuranceDetails);
