# Local Development with Docker
Write-Host "🏠 Starting Notification Service Locally with Docker" -ForegroundColor Green

# Build and run the application
Write-Host "📦 Building Docker image..." -ForegroundColor Yellow
docker build -t notificationservice:latest .

Write-Host "🚀 Starting services with Docker Compose..." -ForegroundColor Yellow
docker-compose up -d

Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "✅ Services started!" -ForegroundColor Green
Write-Host "🌐 Application: http://localhost:8080" -ForegroundColor Cyan
Write-Host "📚 Swagger UI: http://localhost:8080/swagger" -ForegroundColor Cyan
Write-Host "❤️ Health Check: http://localhost:8080/health" -ForegroundColor Cyan
Write-Host "📊 Dashboard: http://localhost:8080/dashboard" -ForegroundColor Cyan

Write-Host "`n🛑 To stop services: docker-compose down" -ForegroundColor Yellow
Write-Host "📝 View logs: docker-compose logs -f" -ForegroundColor Yellow




