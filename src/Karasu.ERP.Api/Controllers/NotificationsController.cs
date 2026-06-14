using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using Karasu.ERP.Application.Features.Notifications.Commands.MarkNotificationRead;
using Karasu.ERP.Application.Features.Notifications.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(page, pageSize, isRead), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPatch("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Bildirim okundu." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPatch("notifications/read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { updatedCount = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}
