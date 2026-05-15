using CliniKey.API.Extensions;
using CliniKey.Application.Features.Appointments.Commands.ChangeStatus;
using CliniKey.Application.Features.Appointments.Commands.ScheduleAppointment;
using CliniKey.Application.Features.Appointments.Queries.GetAppointmentById;
using CliniKey.Application.Features.Appointments.Queries.ListAppointments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly ISender _sender;

    public AppointmentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] ScheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAppointmentByIdQuery(id);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? patientId, [FromQuery] Guid? dentistId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = new ListAppointmentsQuery(patientId, dentistId, startDate, endDate, page, pageSize);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeAppointmentStatusCommand command, CancellationToken cancellationToken)
    {
        if (id != command.AppointmentId)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation.Error",
                Detail = "The ID in the route must match the ID in the request body."
            });
        }

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return NoContent();
    }
}
