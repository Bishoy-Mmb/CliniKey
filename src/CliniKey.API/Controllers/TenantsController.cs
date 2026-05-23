using CliniKey.API.Extensions;
using CliniKey.Application.Constants;
using CliniKey.Application.Features.Tenants.Commands.ActivateClinic;
using CliniKey.Application.Features.Tenants.Commands.DeactivateClinic;
using CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;
using CliniKey.Application.Features.Tenants.Commands.OnboardClinic;
using CliniKey.Application.Features.Tenants.Commands.UpdateClinicContact;
using CliniKey.Application.Features.Tenants.Queries.GetClinicById;
using CliniKey.Application.Features.Tenants.Queries.GetTenantSchemaHealth;
using CliniKey.Application.Features.Tenants.Queries.ListClinics;
using CliniKey.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Controllers;

[ApiController]
[Route("api/v1/tenants/clinics")]
[Authorize(Policy = Policies.CanManageTenants)]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> OnboardClinic(
        [FromBody] OnboardClinicCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(
            nameof(GetClinicById),
            new { clinicId = result.Value.ClinicId },
            result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ListClinics(
        [FromQuery] ClinicStatus? status,
        [FromQuery] TenantSchemaHealthStatus? health,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListClinicsQuery(status, health, page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{clinicId:guid}")]
    public async Task<IActionResult> GetClinicById(
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetClinicByIdQuery(clinicId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{clinicId:guid}/contact")]
    public async Task<IActionResult> UpdateClinicContact(
        Guid clinicId,
        [FromBody] UpdateClinicContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateClinicContactCommand(clinicId, request.Phone, request.Address),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    [HttpPost("{clinicId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateClinic(
        Guid clinicId,
        [FromBody] DeactivateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeactivateClinicCommand(clinicId, request.Reason),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    [HttpPost("{clinicId:guid}/activate")]
    public async Task<IActionResult> ActivateClinic(
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateClinicCommand(clinicId), cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    public sealed record DeactivateClinicRequest(string? Reason);
    public sealed record UpdateClinicContactRequest(string Phone, string Address);
}

[ApiController]
[Route("api/v1/tenants/migrations")]
[Authorize(Policy = Policies.CanManageTenants)]
public sealed class TenantMigrationsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantMigrationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(
        [FromBody] MigrateTenantSchemasCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsSuccess && result.Value.Results.Any(r => r.Status == "Failed"))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, result.Value);
        }

        return result.ToActionResult();
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantSchemaHealthQuery(), cancellationToken);

        return result.ToActionResult();
    }
}
