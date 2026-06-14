using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Suppliers.Commands.CreatePurchaseOrder;
using Karasu.ERP.Application.Features.Suppliers.Commands.CreateSupplier;
using Karasu.ERP.Application.Features.Suppliers.Commands.ReceivePurchaseOrder;
using Karasu.ERP.Application.Features.Suppliers.Commands.UpdateSupplier;
using Karasu.ERP.Application.Features.Suppliers.Queries.GetPurchaseOrderById;
using Karasu.ERP.Application.Features.Suppliers.Queries.GetPurchaseOrders;
using Karasu.ERP.Application.Features.Suppliers.Queries.GetSupplierPerformance;
using Karasu.ERP.Application.Features.Suppliers.Queries.GetSuppliers;
using Karasu.ERP.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuppliersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("suppliers")]
    [Authorize(Policy = Policies.SupplierView)]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSuppliersQuery(page, pageSize, search, isActive), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("suppliers")]
    [Authorize(Policy = Policies.SupplierCreate)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("suppliers/{id:guid}")]
    [Authorize(Policy = Policies.SupplierUpdate)]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSupplierCommand(
            id, request.Name, request.TaxNumber, request.ContactPerson,
            request.Phone, request.Email, request.Address, request.IsActive), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Tedarikçi güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("suppliers/{id:guid}/performance")]
    [Authorize(Policy = Policies.SupplierView)]
    public async Task<IActionResult> GetSupplierPerformance(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSupplierPerformanceQuery(id), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : NotFound(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("purchase-orders")]
    [Authorize(Policy = Policies.PurchaseOrderView)]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] PurchaseOrderStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery(page, pageSize, supplierId, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("purchase-orders/{id:guid}")]
    [Authorize(Policy = Policies.PurchaseOrderView)]
    public async Task<IActionResult> GetPurchaseOrder(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPurchaseOrderByIdQuery(id), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : NotFound(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("purchase-orders")]
    [Authorize(Policy = Policies.PurchaseOrderCreate)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPatch("purchase-orders/{id:guid}/receive")]
    [Authorize(Policy = Policies.PurchaseOrderReceive)]
    public async Task<IActionResult> ReceivePurchaseOrder(Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReceivePurchaseOrderCommand(id, request.WarehouseId, request.Lines), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Mal kabul tamamlandı." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateSupplierRequest(
    string Name,
    string? TaxNumber,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive);

public record ReceivePurchaseOrderRequest(
    Guid WarehouseId,
    IReadOnlyList<ReceivePurchaseOrderLineRequest> Lines);
