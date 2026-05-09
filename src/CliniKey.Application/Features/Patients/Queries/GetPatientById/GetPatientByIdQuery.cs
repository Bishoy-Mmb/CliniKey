using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;

namespace CliniKey.Application.Features.Patients.Queries.GetPatientById;

public sealed record GetPatientByIdQuery(Guid PatientId) : IQuery<PatientResponse>;
