using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Interface for AI-powered text analysis services
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Analyzes notification content using AI to extract insights
    /// </summary>
    /// <param name="content">The notification content to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI analysis results including sentiment, language, and key phrases</returns>
    Task<AIAnalysisResult> AnalyzeNotificationAsync(string content, CancellationToken cancellationToken = default);
}
