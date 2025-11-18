# NotificationService - Complete DevOps & Azure Deployment

A comprehensive notification service built with .NET 8, featuring advanced security, real-time monitoring, and a complete DevOps pipeline for Azure deployment.

## 🚀 Features

- **Multi-Channel Notifications**: Email, SMS, Push, and Webhook notifications
- **Advanced Security**: JWT authentication, role-based authorization, rate limiting, audit logging
- **Real-Time Dashboard**: SignalR-powered monitoring with live metrics
- **Resilience Patterns**: Circuit breaker, retry policies, Polly integration
- **Performance Optimization**: Two-level caching (memory + Redis), async operations
- **Feature Flags**: Azure App Configuration integration for controlled rollouts

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Presentation  │    │  Application    │    │ Infrastructure  │
│   (API, SignalR)│    │   (Services)    │    │   (External)    │
│                 │    │                 │    │                 │
│ • Controllers   │    │ • Notification  │    │ • Email/SMS     │
│ • DTOs          │    │   Strategies    │    │ • Redis Cache   │
│ • Middleware    │    │ • Decorators    │    │ • SQL Database  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 🐳 Local Development with Docker

### Prerequisites
- Docker Desktop
- .NET 8 SDK (for local development)

### Quick Start

1. **Clone and navigate**:
   ```bash
   git clone <repository-url>
   cd NotificationService
   ```

2. **Start services**:
   ```bash
   docker-compose up -d
   ```

3. **Access the application**:
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080
   - Dashboard: http://localhost:8080/dashboard

4. **Database connection**:
   - Server: localhost,1433
   - Database: NotificationDb
   - User: sa
   - Password: YourStrong!Passw0rd

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Staging/Production) | Development |
| `ConnectionStrings__NotificationDb` | SQL Server connection string | Required |
| `ConnectionStrings__Redis` | Redis connection string | localhost:6379 |
| `Jwt__Secret` | JWT signing secret | Required |
| `AzureAppConfig__ConnectionString` | Azure App Configuration connection | Optional |

### Feature Flags

Configure feature flags in Azure App Configuration:
- `EmailNotifications`: Enable/disable email notifications
- `SmsNotifications`: Enable/disable SMS notifications
- `PushNotifications`: Enable/disable push notifications
- `RealTimeDashboard`: Enable/disable dashboard features

## 🚀 Azure Deployment

### Infrastructure as Code

Deploy Azure resources using Bicep:

```bash
# Deploy to staging
az deployment group create \
  --resource-group notification-staging-rg \
  --template-file infra/main.bicep \
  --parameters environmentName=staging

# Deploy to production
az deployment group create \
  --resource-group notification-prod-rg \
  --template-file infra/main.bicep \
  --parameters environmentName=prod
```

### CI/CD Pipeline

The GitHub Actions workflow provides:

1. **Build & Test**: Unit tests, integration tests, security scanning
2. **Staging Deployment**: Blue-green deployment to staging slot
3. **Production Deployment**: Zero-downtime deployment with database migrations

#### Deployment Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   develop   │ -> │   staging   │ -> │ production  │
│   branch    │    │   slot      │    │   slot      │
│             │    │ (blue-green)│    │             │
└─────────────┘    └─────────────┘    └─────────────┘
```

### Database Migrations

Zero-downtime migrations are applied automatically:

```bash
# Apply migrations before swapping slots
./scripts/apply-migrations.ps1 \
  -ResourceGroupName "notification-prod-rg" \
  -AppServiceName "notificationservice-prod-app" \
  -SlotName "staging"
```

## 🔒 Security Features

- **JWT Authentication**: Bearer token authentication
- **Role-Based Authorization**: Admin/User policies
- **Rate Limiting**: IP-based request throttling
- **Security Headers**: XSS protection, CSRF prevention
- **Audit Logging**: Comprehensive security event tracking
- **HTTPS Enforcement**: HSTS and redirection

## 📊 Monitoring & Observability

- **Application Insights**: Performance monitoring and logging
- **Health Checks**: API endpoints for service health
- **Real-Time Metrics**: Live dashboard with SignalR
- **Custom Metrics**: Notification throughput, success rates

## 🧪 Testing

### Unit Tests
```bash
dotnet test tests/NotificationService.UnitTests/
```

### Integration Tests
```bash
dotnet test tests/NotificationService.IntegrationTests/
```

### Core Tests
```bash
dotnet test tests/Core.Tests/
```

## 📋 API Documentation

Access Swagger UI at: `http://localhost:8080`

### Authentication
```bash
# Get JWT token
curl -X POST "http://localhost:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Use token in requests
curl -X POST "http://localhost:8080/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":1,"to":"user@example.com","body":"Hello World!"}'
```

## 🛠️ Development

### Prerequisites
- .NET 8 SDK
- SQL Server (local or Docker)
- Redis (local or Docker)
- Azure CLI (for deployment)

### Local Setup
```bash
# Restore packages
dotnet restore

# Run migrations (if using local SQL Server)
dotnet ef database update

# Run application
dotnet run --urls="http://localhost:5268"
```

### Adding New Features
1. Create feature flag in Azure App Configuration
2. Implement feature toggle in code using `IFeatureManager`
3. Update configuration and documentation

## 📚 Project Structure

```
├── src/
│   ├── Core/                    # Domain layer
│   ├── NotificationService/     # Application layer
│   │   ├── Controllers/         # API endpoints
│   │   ├── Services/           # Business logic
│   │   ├── Strategies/         # Notification implementations
│   │   ├── Data/               # EF Core context & migrations
│   │   └── Configurations/     # App settings
│   └── tests/                  # Unit & integration tests
├── infra/                      # Azure Bicep templates
├── scripts/                    # Deployment scripts
└── .github/workflows/          # CI/CD pipelines
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

- **Issues**: Create GitHub issues for bugs and feature requests
- **Documentation**: See inline code comments and XML documentation
- **Security**: Report security vulnerabilities privately

---

**Built with ❤️ using .NET 8, Azure, and Docker**
