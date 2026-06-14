using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Queries.ExportProducts;

public record ExportProductsQuery : IRequest<Result<ExportProductsResult>>;

public record ExportProductsResult(byte[] Content, string FileName, string ContentType);

public class ExportProductsQueryHandler : IRequestHandler<ExportProductsQuery, Result<ExportProductsResult>>
{
    private readonly IProductExcelService _excelService;
    private readonly ITenantContext _tenantContext;

    public ExportProductsQueryHandler(IProductExcelService excelService, ITenantContext tenantContext)
    {
        _excelService = excelService;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ExportProductsResult>> Handle(ExportProductsQuery request, CancellationToken cancellationToken)
    {
        var bytes = await _excelService.ExportAsync(_tenantContext.TenantId, cancellationToken);
        var fileName = $"urunler-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return Result<ExportProductsResult>.Success(
            new ExportProductsResult(bytes, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }
}
