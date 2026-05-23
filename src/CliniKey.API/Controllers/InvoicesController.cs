using CliniKey.API.Contracts.Invoices;
using CliniKey.API.Extensions;
using CliniKey.Application.Constants;
using CliniKey.Application.Features.Invoices.Commands.CreateInvoice;
using CliniKey.Application.Features.Invoices.Commands.IssueInvoice;
using CliniKey.Application.Features.Invoices.Commands.RecordPayment;
using CliniKey.Application.Features.Invoices.Commands.VoidInvoice;
using CliniKey.Application.Features.Invoices.Queries.GetInvoiceById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKey.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = Policies.CanManageBilling)]
public sealed class InvoicesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateInvoiceCommand request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
    }

    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        var command = new RecordPaymentCommand(id, request.Amount, request.Currency, request.Method, request.ReferenceNumber);
        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id }, result.Value)
            : result.ToActionResult();
    }

    [HttpPatch("{id:guid}/issue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new IssueInvoiceCommand(id), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }

    [HttpPatch("{id:guid}/void")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new VoidInvoiceCommand(id), ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }
}
