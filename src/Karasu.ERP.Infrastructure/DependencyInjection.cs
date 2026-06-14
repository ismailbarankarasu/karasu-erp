using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Infrastructure.Caching;
using Karasu.ERP.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karasu.ERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddHttpContextAccessor();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<ITenantNotificationPublisher, TenantNotificationPublisher>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IReceiptPdfService, ReceiptPdfService>();
        services.AddScoped<IReportExportService, ReportExportService>();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
