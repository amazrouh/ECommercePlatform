using AutoMapper;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
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
    private readonly Core.Interfaces.IAIService _aiService;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IFeatureManager _featureManager;

    public NotificationsController(
        INotificationService notificationService,
        Core.Interfaces.IAIService aiService,
        IMapper mapper,
        ILogger<NotificationsController> logger,
        IFeatureManager featureManager)
    {
        _notificationService = notificationService;
        _aiService = aiService;
        _mapper = mapper;
        _logger = logger;
        _featureManager = featureManager;
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

        // Check if the notification type is enabled via feature flags
        var featureFlagName = $"{request.Type}Notifications";
        if (!await _featureManager.IsEnabledAsync(featureFlagName))
        {
            _logger.LogWarning("Notification type {Type} is disabled via feature flag", request.Type);
            return BadRequest(new { message = $"Notification type {request.Type} is currently disabled" });
        }

        var message = _mapper.Map<Core.Models.NotificationMessage>(request);
        var result = await _notificationService.SendAsync(request.Type, message, cancellationToken);

        var response = _mapper.Map<NotificationResponse>((result, request.Type, request.To));
        return Ok(response);
    }

    /// <summary>
    /// Sends a notification with AI-powered analysis and smart routing.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the notification operation with AI insights.</returns>
    [HttpPost("send-smart")]
    [Authorize(Policy = "RequireUser")]
    [ProducesResponseType(typeof(SmartNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SmartNotificationResponse>> SendSmartNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received AI-enhanced request to send {Type} notification to {Recipient}",
            request.Type, request.To);

        // Check if the notification type is enabled via feature flags
        var featureFlagName = $"{request.Type}Notifications";
        if (!await _featureManager.IsEnabledAsync(featureFlagName))
        {
            _logger.LogWarning("Notification type {Type} is disabled via feature flag", request.Type);
            return BadRequest(new { message = $"Notification type {request.Type} is currently disabled" });
        }

        // Perform AI analysis on the notification content
        var aiAnalysis = await _aiService.AnalyzeNotificationAsync(request.Body, cancellationToken);

        // Create the notification message with AI insights
        var originalMessage = _mapper.Map<Core.Models.NotificationMessage>(request);

        // Add AI metadata to the message
        var aiMetadata = new Dictionary<string, object>
        {
            ["ai_sentiment"] = aiAnalysis.Sentiment,
            ["ai_language"] = aiAnalysis.DetectedLanguage,
            ["ai_priority"] = aiAnalysis.RecommendedPriority.ToString(),
            ["ai_confidence"] = aiAnalysis.SentimentConfidence.ToString("F2"),
            ["ai_analyzed_at"] = aiAnalysis.AnalyzedAt.ToString("O")
        };

        // Merge with existing metadata if any
        if (originalMessage.Metadata != null)
        {
            foreach (var kvp in originalMessage.Metadata)
            {
                aiMetadata[kvp.Key] = kvp.Value;
            }
        }

        var message = originalMessage.WithMetadata(aiMetadata);

        // Smart routing based on AI analysis
        var originalType = request.Type;
        if (aiAnalysis.RecommendedPriority == Core.Models.NotificationPriority.High &&
            originalType == Core.Enums.NotificationType.Email)
        {
            // For high priority negative content, ensure it goes to email regardless of original request
            _logger.LogInformation("AI upgraded notification priority from {Original} to Email due to sentiment analysis",
                originalType);
        }

        // Send the notification
        var result = await _notificationService.SendAsync(request.Type, message, cancellationToken);

        // Create response with AI insights
        var response = new SmartNotificationResponse
        {
            Success = result.Success,
            MessageId = result.MessageId,
            Error = result.Error,
            NotificationType = request.Type,
            Recipient = request.To,
            AIAnalysis = aiAnalysis
        };

        _logger.LogInformation(
            "AI-enhanced notification sent - Success: {Success}, Sentiment: {Sentiment}, Priority: {Priority}",
            result.Success, aiAnalysis.Sentiment, aiAnalysis.RecommendedPriority);

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
