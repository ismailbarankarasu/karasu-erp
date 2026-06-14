using System.Text;
using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Api.Middleware;
using Karasu.ERP.Application;
using Karasu.ERP.Identity;
using Karasu.ERP.Identity.Options;
using Karasu.ERP.Infrastructure;
using Karasu.ERP.Infrastructure.Services;
using Karasu.ERP.Persistence;
using Karasu.ERP.Persistence.Configuration;
using Karasu.ERP.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtServices(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options => options.AddPermissionPolicies());
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddApiPerformance();
builder.Services.AddControllers();
builder.Services.AddApiSwagger(builder.Configuration);

var redisConnection = builder.Configuration.GetConnectionString("Redis");
var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
var dbConnection = databaseOptions.ConnectionString ?? builder.Configuration.GetConnectionString("DefaultConnection");

var healthChecksBuilder = builder.Services.AddHealthChecks();

if (!builder.Environment.IsEnvironment("Testing") && !string.IsNullOrEmpty(dbConnection))
{
    if (databaseOptions.Provider == DatabaseProvider.PostgreSQL)
        healthChecksBuilder.AddNpgSql(dbConnection);
    else
        healthChecksBuilder.AddSqlServer(dbConnection);
}

if (!string.IsNullOrEmpty(redisConnection) && !builder.Environment.IsEnvironment("Testing"))
    healthChecksBuilder.AddRedis(redisConnection);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
    await DatabaseSeeder.SeedAsync(app.Services);

app.UseApiSwagger();

app.UseResponseCompression();
app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program { }
