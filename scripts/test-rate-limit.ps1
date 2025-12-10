Write-Host "Testing rate limiting with 100 rapid requests..." -ForegroundColor Cyan
Write-Host "Rate limiting should be COMPLETELY DISABLED in Development" -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$rateLimitCount = 0

for ($i = 1; $i -le 100; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/api/demo/security/rate-limiting" -Method GET -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $successCount++
            if ($i -le 10 -or $i % 10 -eq 0) {
                Write-Host "Request $i : Success (200)" -ForegroundColor Green
            }
        }
        elseif ($response.StatusCode -eq 429) {
            $rateLimitCount++
            Write-Host "Request $i : Rate Limited (429)" -ForegroundColor Yellow
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 429) {
            $rateLimitCount++
            Write-Host "Request $i : Rate Limited (429)" -ForegroundColor Yellow
        }
        else {
            Write-Host "Request $i : Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    # No delay between requests to really test rate limiting
}

Write-Host ""
Write-Host "Results:" -ForegroundColor Cyan
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Rate Limited: $rateLimitCount" -ForegroundColor Yellow

if ($rateLimitCount -eq 0) {
    Write-Host ""
    Write-Host "✅ Rate limiting COMPLETELY DISABLED for testing!" -ForegroundColor Green
    Write-Host "You can now run load tests without any rate limiting." -ForegroundColor White
}
else {
    Write-Host ""
    Write-Host "⚠️ Rate limiting still active. Check configuration." -ForegroundColor Yellow
}
