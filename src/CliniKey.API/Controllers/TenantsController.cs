using CliniKey.API.Extensions;
using CliniKey.Application.Constants;
using CliniKey.Application.Features.Tenants.Commands.ActivateTenant;
using CliniKey.Application.Features.Tenants.Commands.DeactivateTenant;
using CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;
using CliniKey.Application.Features.Tenants.Commands.OnboardTenant;
using CliniKey.Application.Features.Tenants.Commands.UpdateClinicContact;
using CliniKey.Application.Features.Tenants.Queries.GetTenantById;
using CliniKey.Application.Features.Tenants.Queries.GetTenantSchemaHealth;
using CliniKey.Application.Features.Tenants.Queries.ListTenants;
using CliniKey.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Controllers;

[ApiController]
[Route("api/v1/tenants")]
[Authorize(Policy = Policies.CanManageTenants)]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> OnboardTenant(
        [FromBody] OnboardTenantCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(
            nameof(GetTenantById),
            new { tenantId = result.Value.TenantId },
            result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ListTenants(
        [FromQuery] TenantStatus? status,
        [FromQuery] TenantSchemaHealthStatus? health,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListTenantsQuery(status, health, page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> GetTenantById(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantByIdQuery(tenantId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{tenantId:guid}/clinics/{clinicId:guid}/contact")]
    public async Task<IActionResult> UpdateClinicContact(
        Guid tenantId,
        Guid clinicId,
        [FromBody] UpdateClinicContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateClinicContactCommand(tenantId, clinicId, request.Phone, request.Address),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    [HttpPost("{tenantId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(
        Guid tenantId,
        [FromBody] DeactivateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeactivateTenantCommand(tenantId, request.Reason),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    [HttpPost("{tenantId:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateTenantCommand(tenantId), cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    public sealed record DeactivateTenantRequest(string? Reason);
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
