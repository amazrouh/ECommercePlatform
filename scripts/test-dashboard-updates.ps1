Write-Host "Testing dashboard updates..." -ForegroundColor Cyan
Write-Host "Sending 5 test notifications to generate metrics..." -ForegroundColor Yellow

# Authenticate first
$authBody = '{"username":"admin","password":"admin123"}'
$authResponse = Invoke-WebRequest -Uri "http://localhost:8080/api/auth/login" -Method POST -Body $authBody -ContentType "application/json"
$authData = $authResponse.Content | ConvertFrom-Json
$jwtToken = $authData.token

Write-Host "✓ Authenticated successfully" -ForegroundColor Green

# Send notifications
for ($i = 1; $i -le 5; $i++) {
    $notificationBody = "{`"type`":1,`"to`":`"test$i@example.com`",`"subject`":`"Test $i`",`"body`":`"Dashboard test notification $i`"}"
    $headers = @{"Authorization" = "Bearer $jwtToken"; "Content-Type" = "application/json"}

    $response = Invoke-WebRequest -Uri "http://localhost:8080/api/notifications" -Method POST -Headers $headers -Body $notificationBody
    Write-Host "✓ Notification $i sent (Status: $($response.StatusCode))" -ForegroundColor Green

    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "✅ Test complete! Check your dashboard at: http://localhost:8080/dashboard" -ForegroundColor Cyan
Write-Host "   - Refresh the page if needed" -ForegroundColor White
Write-Host "   - You should see updated metrics" -ForegroundColor White
