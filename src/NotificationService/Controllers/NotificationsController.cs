using AutoMapper;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;

namespace NotificationService.Controllers;

/// <summary>
/// Controller for managing notifications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IMapper mapper,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the notification operation.</returns>
    [HttpPost]
    [Authorize(Policy = "RequireUser")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NotificationResponse>> SendNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received request to send {Type} notification to {Recipient}",
            request.Type, request.To);

        var message = _mapper.Map<Core.Models.NotificationMessage>(request);
        var result = await _notificationService.SendAsync(request.Type, message, cancellationToken);

        var response = _mapper.Map<NotificationResponse>((result, request.Type, request.To));
        return Ok(response);
    }

    /// <summary>
    /// Gets all supported notification types.
    /// </summary>
    /// <returns>List of supported notification types.</returns>
    [HttpGet("types")]
    [Authorize(Policy = "RequireUser")]
    [ProducesResponseType(typeof(IEnumerable<Core.Enums.NotificationType>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Core.Enums.NotificationType>>> GetNotificationTypes()
    {
        var types = await _notificationService.GetSupportedTypes();
        return Ok(types);
    }

    /// <summary>
    /// Gets the health status of the notification service.
    /// </summary>
    /// <returns>Health check result.</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var types = await _notificationService.GetSupportedTypes();
            if (!types.Any())
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { status = "degraded", message = "No notification types available" });
            }

            return Ok(new { status = "healthy", supportedTypes = types });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unhealthy", message = "Service check failed" });
        }
    }
}
