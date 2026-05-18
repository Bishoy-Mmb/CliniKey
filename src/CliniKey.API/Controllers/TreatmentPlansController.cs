using CliniKey.Application.Features.TreatmentPlans.Commands.ApproveTreatmentPlan;
using CliniKey.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;
using CliniKey.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;
using CliniKey.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "ClinicAdmin,Dentist")]
public sealed class TreatmentPlansController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateTreatmentPlanCommand request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CliniKey.Application.DTOs.TreatmentPlanResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetTreatmentPlanByIdQuery(id);
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
    }

    [HttpPatch("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var command = new ApproveTreatmentPlanCommand(id);
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }
}
