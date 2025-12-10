param(
    [string]$BaseUrl = "http://localhost:8080"
)

Write-Host "🚀 Notification Service Local Testing Guide" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Quick health check
Write-Host "Testing basic connectivity..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/health" -Method GET
    Write-Host "✓ Health check passed (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🔗 Open these URLs in your browser:" -ForegroundColor Cyan
Write-Host "  🌐 Swagger UI: $BaseUrl" -ForegroundColor White
Write-Host "  📊 Dashboard: $BaseUrl/dashboard" -ForegroundColor White
Write-Host "  🔧 SignalR Debug: $BaseUrl/debug" -ForegroundColor White
Write-Host "  💚 Health Check: $BaseUrl/health" -ForegroundColor White

Write-Host ""
Write-Host "📝 Manual Testing Steps:" -ForegroundColor Cyan
Write-Host "  1. Open Swagger UI and try authentication" -ForegroundColor White
Write-Host "  2. Send test notifications via the API" -ForegroundColor White
Write-Host "  3. Check the dashboard for real-time metrics" -ForegroundColor White
Write-Host "  4. Use SignalR debug page to test connections" -ForegroundColor White
Write-Host "  5. Check database for notification persistence" -ForegroundColor White
