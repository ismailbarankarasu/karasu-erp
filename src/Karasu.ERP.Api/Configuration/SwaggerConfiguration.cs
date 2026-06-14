using Microsoft.OpenApi.Models;

namespace Karasu.ERP.Api.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Karasu ERP API",
                Version = "v1",
                Description = "Karasu ERP REST API — POS, stok, sipariş, finans, raporlama, HR ve e-fatura modülleri.",
                Contact = new OpenApiContact
                {
                    Name = "Karasu ERP",
                    Email = "support@karasuerp.com",
                    Url = new Uri("https://github.com/ismailbarankarasu/karasu-erp")
                },
                License = new OpenApiLicense
                {
                    Name = "Proprietary"
                }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Bearer token. Örnek: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.TagActionsBy(api =>
            {
                if (api.GroupName is not null)
                    return [api.GroupName];

                var controller = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var name)
                    ? name
                    : "Default";
                return [controller];
            });

            options.DocInclusionPredicate((_, _) => true);
            options.SupportNonNullableReferenceTypes();
        });

        return services;
    }

    public static void UseApiSwagger(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() &&
            !app.Environment.IsStaging() &&
            !app.Environment.IsEnvironment("Testing"))
            return;

        app.UseSwagger(options =>
        {
            options.RouteTemplate = "openapi/{documentName}/openapi.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1/openapi.json", "Karasu ERP API v1");
            options.DocumentTitle = "Karasu ERP API";
            options.DisplayRequestDuration();
        });
    }
}
