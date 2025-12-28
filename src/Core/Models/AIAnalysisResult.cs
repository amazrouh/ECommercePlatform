namespace Core.Models;

/// <summary>
/// Result of AI analysis on notification content
/// </summary>
public class AIAnalysisResult
{
    /// <summary>
    /// Sentiment analysis result (Positive, Negative, Neutral, Mixed)
    /// </summary>
    public string Sentiment { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for the sentiment analysis (0.0 to 1.0)
    /// </summary>
    public double SentimentConfidence { get; set; }

    /// <summary>
    /// Detected language of the content (ISO 639-1 format, e.g., "en", "ar")
    /// </summary>
    public string DetectedLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for language detection (0.0 to 1.0)
    /// </summary>
    public double LanguageConfidence { get; set; }

    /// <summary>
    /// Key phrases extracted from the content
    /// </summary>
    public string[] KeyPhrases { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Recommended notification priority based on AI analysis
    /// </summary>
    public NotificationPriority RecommendedPriority { get; set; }

    /// <summary>
    /// AI processing timestamp
    /// </summary>
    public DateTimeOffset AnalyzedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the AI analysis was successful
    /// </summary>
    public bool AnalysisSuccessful { get; set; } = true;

    /// <summary>
    /// Error message if analysis failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Recommended notification priority levels
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority - informational content
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority - standard notifications
    /// </summary>
    Normal,

    /// <summary>
    /// High priority - urgent or negative content
    /// </summary>
    High,

    /// <summary>
    /// Critical priority - immediate attention required
    /// </summary>
    Critical
}