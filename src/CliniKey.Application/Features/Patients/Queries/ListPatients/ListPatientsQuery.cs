using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;

namespace CliniKey.Application.Features.Patients.Queries.ListPatients;

public sealed record ListPatientsQuery(
    string? SearchTerm,
    int Page = 1,
    int PageSize = 10) : IQuery<IReadOnlyList<PatientResponse>>;
