using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Products.Commands.CreateBrand;
using Karasu.ERP.Application.Features.Products.Commands.CreateCategory;
using Karasu.ERP.Application.Features.Products.Commands.CreateProduct;
using Karasu.ERP.Application.Features.Products.Commands.CreateProductVariant;
using Karasu.ERP.Application.Features.Products.Commands.DeleteProduct;
using Karasu.ERP.Application.Features.Products.Commands.DeleteProductVariant;
using Karasu.ERP.Application.Features.Products.Commands.GenerateBarcode;
using Karasu.ERP.Application.Features.Products.Commands.GenerateQrCode;
using Karasu.ERP.Application.Features.Products.Commands.ImportProducts;
using Karasu.ERP.Application.Features.Products.Commands.UpdateProduct;
using Karasu.ERP.Application.Features.Products.Commands.UpdateProductVariant;
using Karasu.ERP.Application.Features.Products.Queries.ExportProducts;
using Karasu.ERP.Application.Features.Products.Queries.GetBrands;
using Karasu.ERP.Application.Features.Products.Queries.GetCategories;
using Karasu.ERP.Application.Features.Products.Queries.GetProductByBarcode;
using Karasu.ERP.Application.Features.Products.Queries.GetProductById;
using Karasu.ERP.Application.Features.Products.Queries.GetProducts;
using Karasu.ERP.Application.Features.Products.Queries.GetUnits;
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
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("products")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] ProductStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, pageSize, search, categoryId, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("products/{id:guid}")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpGet("products/barcode/{barcode}")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> GetProductByBarcode(string barcode, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByBarcodeQuery(barcode), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("products")]
    [Authorize(Policy = Policies.ProductCreate)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProduct), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("products/{id:guid}")]
    [Authorize(Policy = Policies.ProductUpdate)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProductCommand(
            id,
            request.Sku,
            request.Barcode,
            request.Name,
            request.CategoryId,
            request.BrandId,
            request.UnitId,
            request.PurchasePrice,
            request.SalePrice,
            request.TaxRate,
            request.MinStock,
            request.Status), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Ürün güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpDelete("products/{id:guid}")]
    [Authorize(Policy = Policies.ProductDelete)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(new { message = "Ürün silindi." }));
    }

    [HttpGet("categories")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("categories")]
    [Authorize(Policy = Policies.ProductCreate)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("brands")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> GetBrands(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBrandsQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("brands")]
    [Authorize(Policy = Policies.ProductCreate)]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("units")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> GetUnits(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUnitsQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("products/import")]
    [Authorize(Policy = Policies.ProductCreate)]
    public async Task<IActionResult> ImportProducts(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(WrapError("Dosya boş.", "EMPTY_FILE"));

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportProductsCommand(stream), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("products/export")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> ExportProducts(CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportProductsQuery(), ct);
        if (!result.IsSuccess) return BadRequest(WrapError(result.Error!, result.ErrorCode));
        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("products/{id:guid}/barcode/generate")]
    [Authorize(Policy = Policies.ProductUpdate)]
    public async Task<IActionResult> GenerateBarcode(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateProductBarcodeCommand(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("products/{id:guid}/qrcode/generate")]
    [Authorize(Policy = Policies.ProductView)]
    public async Task<IActionResult> GenerateQrCode(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateProductQrCodeCommand(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("products/{id:guid}/variants")]
    [Authorize(Policy = Policies.ProductCreate)]
    public async Task<IActionResult> CreateVariant(Guid id, [FromBody] CreateVariantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateProductVariantCommand(
            id,
            request.Sku,
            request.Barcode,
            request.PurchasePrice,
            request.SalePrice,
            request.AttributesJson), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("products/{id:guid}/variants/{variantId:guid}")]
    [Authorize(Policy = Policies.ProductUpdate)]
    public async Task<IActionResult> UpdateVariant(Guid id, Guid variantId, [FromBody] UpdateVariantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProductVariantCommand(
            id,
            variantId,
            request.Sku,
            request.Barcode,
            request.PurchasePrice,
            request.SalePrice,
            request.AttributesJson), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Varyant güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpDelete("products/{id:guid}/variants/{variantId:guid}")]
    [Authorize(Policy = Policies.ProductDelete)]
    public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductVariantCommand(id, variantId), ct);
        if (!result.IsSuccess) return BadRequest(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(new { message = "Varyant silindi." }));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateProductRequest(
    string Sku,
    string? Barcode,
    string Name,
    Guid? CategoryId,
    Guid? BrandId,
    Guid UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal TaxRate,
    decimal MinStock,
    ProductStatus Status);

public record CreateVariantRequest(
    string Sku,
    string? Barcode,
    decimal PurchasePrice,
    decimal SalePrice,
    string AttributesJson);

public record UpdateVariantRequest(
    string Sku,
    string? Barcode,
    decimal PurchasePrice,
    decimal SalePrice,
    string AttributesJson);
