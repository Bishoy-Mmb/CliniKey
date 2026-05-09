using CliniKey.API.Extensions;
using CliniKey.Application.Features.Patients.Commands.CreatePatient;
using CliniKey.Application.Features.Patients.Commands.DeletePatient;
using CliniKey.Application.Features.Patients.Commands.UpdatePatient;
using CliniKey.Application.Features.Patients.Queries.GetPatientById;
using CliniKey.Application.Features.Patients.Queries.ListPatients;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class PatientsController : ControllerBase
{
    private readonly ISender _sender;

    public PatientsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePatient(
        [FromBody] CreatePatientCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(
            nameof(GetPatientById),
            new { id = result.Value },
            result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPatientById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetPatientByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> ListPatients(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new ListPatientsQuery(searchTerm, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePatient(
        Guid id,
        [FromBody] UpdatePatientCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.PatientId)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation.Error",
                Detail = "The ID in the route must match the ID in the request body."
            });
        }

        var result = await _sender.Send(command, cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePatient(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeletePatientCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        return result.ToActionResult();
    }
}
