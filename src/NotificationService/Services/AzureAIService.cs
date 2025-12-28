using Azure;
using Azure.AI.TextAnalytics;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NotificationService.Services;

/// <summary>
/// Azure Cognitive Services implementation for AI-powered text analysis
/// </summary>
public class AzureAIService : IAIService
{
    private readonly TextAnalyticsClient _textAnalyticsClient;
    private readonly ILogger<AzureAIService> _logger;
    private readonly Core.Interfaces.IMetricsRecorder _metricsRecorder;

    public AzureAIService(IConfiguration configuration, ILogger<AzureAIService> logger, Core.Interfaces.IMetricsRecorder metricsRecorder)
    {
        _logger = logger;
        _metricsRecorder = metricsRecorder;

        var endpoint = configuration["AzureAI:Endpoint"];
        var key = configuration["AzureAI:Key"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Azure AI configuration is missing. Set AzureAI:Endpoint and AzureAI:Key in environment variables or user secrets. AI features will be disabled.");
            _textAnalyticsClient = null!;
            return;
        }

        try
        {
            _textAnalyticsClient = new TextAnalyticsClient(
                new Uri(endpoint),
                new AzureKeyCredential(key));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Text Analytics client");
            _textAnalyticsClient = null!;
        }
    }

    /// <inheritdoc />
    public async Task<AIAnalysisResult> AnalyzeNotificationAsync(string content, CancellationToken cancellationToken = default)
    {
        var result = new AIAnalysisResult
        {
            AnalysisSuccessful = false,
            ErrorMessage = "Azure AI service not configured"
        };

        if (_textAnalyticsClient == null || string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        try
        {
            var startTime = DateTimeOffset.UtcNow;
            _logger.LogInformation("Starting AI analysis for notification content (length: {Length})", content.Length);

            // Prepare analysis tasks
            var sentimentTask = AnalyzeSentimentAsync(content, cancellationToken);
            var languageTask = DetectLanguageAsync(content, cancellationToken);
            var keyPhrasesTask = ExtractKeyPhrasesAsync(content, cancellationToken);

            // Execute all analyses in parallel
            await Task.WhenAll(sentimentTask, languageTask, keyPhrasesTask);

            // Build result
            var sentimentResult = await sentimentTask;
            var languageResult = await languageTask;
            var keyPhrasesResult = await keyPhrasesTask;

            result.Sentiment = sentimentResult.Sentiment.ToString();
            result.SentimentConfidence = sentimentResult.ConfidenceScores.Positive;
            result.DetectedLanguage = languageResult.Iso6391Name;
            result.LanguageConfidence = languageResult.ConfidenceScore;
            result.KeyPhrases = keyPhrasesResult.ToArray();
            result.RecommendedPriority = DeterminePriority(sentimentResult, keyPhrasesResult);
            result.AnalysisSuccessful = true;
            result.ErrorMessage = null;

            _logger.LogInformation(
                "AI analysis completed - Sentiment: {Sentiment}, Language: {Language}, Priority: {Priority}",
                result.Sentiment,
                result.DetectedLanguage,
                result.RecommendedPriority);

            // Record AI metrics for dashboard
            if (_metricsRecorder is DashboardMetricsService dashboardMetrics)
            {
                dashboardMetrics.RecordAIMetrics(
                    result.Sentiment,
                    result.DetectedLanguage,
                    (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
            }

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure AI service request failed");
            result.ErrorMessage = $"AI service error: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI analysis");
            result.ErrorMessage = $"Analysis failed: {ex.Message}";
            return result;
        }
    }

    private async Task<DocumentSentiment> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken)
    {
        var response = await _textAnalyticsClient.AnalyzeSentimentAsync(content, cancellationToken: cancellationToken);
        return response.Value;
    }

    private async Task<DetectedLanguage> DetectLanguageAsync(string content, CancellationToken cancellationToken)
    {
        var response = await _textAnalyticsClient.DetectLanguageAsync(content, cancellationToken: cancellationToken);
        return response.Value;
    }

    private async Task<KeyPhraseCollection> ExtractKeyPhrasesAsync(string content, CancellationToken cancellationToken)
    {
        var response = await _textAnalyticsClient.ExtractKeyPhrasesAsync(content, cancellationToken: cancellationToken);
        return response.Value;
    }

    private NotificationPriority DeterminePriority(DocumentSentiment sentiment, KeyPhraseCollection keyPhrases)
    {
        // High priority for negative sentiment
        if (sentiment.Sentiment == TextSentiment.Negative && sentiment.ConfidenceScores.Negative > 0.6)
        {
            return NotificationPriority.High;
        }

        // Check for urgent keywords in key phrases
        var urgentKeywords = new[] { "urgent", "critical", "emergency", "failed", "error", "issue", "problem", "delay" };
        var keyPhraseText = string.Join(" ", keyPhrases).ToLowerInvariant();

        if (urgentKeywords.Any(keyword => keyPhraseText.Contains(keyword)))
        {
            return NotificationPriority.High;
        }

        // Check for positive confirmations
        var positiveKeywords = new[] { "success", "confirmed", "completed", "delivered", "received" };
        if (positiveKeywords.Any(keyword => keyPhraseText.Contains(keyword)))
        {
            return NotificationPriority.Normal;
        }

        // Default priority
        return NotificationPriority.Normal;
    }
}