using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Identity.Options;
using Karasu.ERP.Identity.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karasu.ERP.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<ITokenService, JwtTokenService>();
        return services;
    }
}
