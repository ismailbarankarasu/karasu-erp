using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Commands.ImportProducts;

public record ImportProductsCommand(Stream ExcelStream) : IRequest<Result<ProductImportResult>>;

public class ImportProductsCommandHandler : IRequestHandler<ImportProductsCommand, Result<ProductImportResult>>
{
    private readonly IProductExcelService _excelService;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public ImportProductsCommandHandler(
        IProductExcelService excelService,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _excelService = excelService;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<ProductImportResult>> Handle(ImportProductsCommand request, CancellationToken cancellationToken)
    {
        var result = await _excelService.ImportAsync(request.ExcelStream, _tenantContext.TenantId, cancellationToken);
        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:product:list:*", cancellationToken);
        return Result<ProductImportResult>.Success(result);
    }
}
