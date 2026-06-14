using System.Text.Json;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Infrastructure.Services;

public class StubEInvoiceProvider : IEInvoiceProvider
{
    public EInvoiceProvider ProviderType => EInvoiceProvider.Stub;

    public Task<EInvoiceSubmitResult> SubmitEInvoiceAsync(EInvoiceSubmitRequest request, CancellationToken ct) =>
        Task.FromResult(CreateSuccessResult("EInvoice"));

    public Task<EInvoiceSubmitResult> SubmitEArchiveAsync(EInvoiceSubmitRequest request, CancellationToken ct) =>
        Task.FromResult(CreateSuccessResult("EArchive"));

    public Task<EInvoiceSubmitResult> SubmitEDispatchAsync(EInvoiceSubmitRequest request, CancellationToken ct) =>
        Task.FromResult(CreateSuccessResult("EDispatch"));

    public Task<EInvoiceSubmitResult> CheckStatusAsync(string gibUuid, CancellationToken ct) =>
        Task.FromResult(new EInvoiceSubmitResult(
            true,
            gibUuid,
            JsonSerializer.Serialize(new { status = "Accepted", provider = "Stub" }),
            null));

    private static EInvoiceSubmitResult CreateSuccessResult(string type)
    {
        var gibUuid = $"STUB-{type}-{Guid.NewGuid():N}";
        return new EInvoiceSubmitResult(
            true,
            gibUuid,
            JsonSerializer.Serialize(new { status = "Submitted", provider = "Stub", gibUuid }),
            null);
    }
}

public class EInvoiceProviderResolver : IEInvoiceProviderResolver
{
    private readonly IEnumerable<IEInvoiceProvider> _providers;

    public EInvoiceProviderResolver(IEnumerable<IEInvoiceProvider> providers) => _providers = providers;

    public IEInvoiceProvider Resolve(EInvoiceProvider provider) =>
        _providers.FirstOrDefault(p => p.ProviderType == provider)
        ?? _providers.First(p => p.ProviderType == EInvoiceProvider.Stub);
}
