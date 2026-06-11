using System.Net;
using System.Text.Json;
using FluentValidation;
using Karasu.ERP.Application.Common.Exceptions;
using Karasu.ERP.Application.Common.Interfaces;

namespace Karasu.ERP.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, code, message) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),
            ApplicationValidationException appValEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                string.Join("; ", appValEx.Errors.SelectMany(e => e.Value))),
            NotFoundException notFound => (HttpStatusCode.NotFound, "NOT_FOUND", notFound.Message),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, "FORBIDDEN", forbidden.Message),
            UnauthorizedAccessException unauthorized => (HttpStatusCode.Forbidden, "FORBIDDEN", unauthorized.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Beklenmeyen bir hata oluştu.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success = false,
            data = (object?)null,
            errors = new[] { new { code, message } }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        var isSuperAdmin = context.User.IsInRole("SuperAdmin");

        if (Guid.TryParse(tenantClaim, out var tenantFromClaim))
            tenantContext.TenantId = tenantFromClaim;
        else if (Guid.TryParse(tenantIdHeader, out var tenantFromHeader))
            tenantContext.TenantId = tenantFromHeader;

        tenantContext.IsSuperAdmin = isSuperAdmin;

        await _next(context);
    }
}
