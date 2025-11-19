# Notification Service - API Guide

## 📋 **Overview**

The Notification Service provides a comprehensive REST API for sending notifications across multiple channels (Email, SMS, Push, Webhook) with real-time monitoring and management capabilities.

## 🔗 **Base URL**

```
Production: https://your-app-name.azurewebsites.net
Development: http://localhost:8080
```

## 🔐 **Authentication**

All API endpoints (except health checks) require JWT authentication.

### **Get Access Token**

```bash
curl -X POST "https://your-app/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2025-01-01T12:00:00Z",
  "userId": "admin",
  "roles": ["Admin"]
}
```

### **Using the Token**

Include the token in the Authorization header for all subsequent requests:

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     https://your-app/api/notifications
```

## 📧 **Notifications API**

### **Send a Single Notification**

Send notifications via Email, SMS, Push, or Webhook channels.

```bash
curl -X POST "https://your-app/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "Email",
    "to": "recipient@example.com",
    "subject": "Welcome to our platform!",
    "body": "Thank you for joining us. Your account has been created successfully."
  }'
```

**Parameters:**
- `type`: `"Email"`, `"Sms"`, `"Push"`, or `"Webhook"`
- `to`: Recipient address (email, phone, device token, or URL)
- `subject`: Message subject (not used for SMS)
- `body`: Message content

**Response:**
```json
{
  "id": "EMAIL_abc123def456",
  "type": "Email",
  "to": "recipient@example.com",
  "success": true,
  "timestamp": "2025-01-01T10:30:00Z",
  "errorMessage": null
}
```

### **Send Batch Notifications**

Send multiple notifications of different types in a single request.

```bash
curl -X POST "https://your-app/api/notifications/batch" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "notifications": {
      "Email": {
        "to": "user@example.com",
        "subject": "Order Confirmation",
        "body": "Your order #12345 has been confirmed."
      },
      "Sms": {
        "to": "+1234567890",
        "subject": "",
        "body": "Your verification code is: 123456"
      },
      "Push": {
        "to": "device-token-abc123",
        "subject": "New Message",
        "body": "You have a new message from John Doe"
      }
    }
  }'
```

**Response:**
```json
{
  "Email": {
    "id": "EMAIL_xyz789",
    "type": "Email",
    "to": "user@example.com",
    "success": true,
    "timestamp": "2025-01-01T10:30:00Z",
    "errorMessage": null
  },
  "Sms": {
    "id": "SMS_abc456",
    "type": "Sms",
    "to": "+1234567890",
    "success": true,
    "timestamp": "2025-01-01T10:30:00Z",
    "errorMessage": null
  },
  "Push": {
    "id": "PUSH_def789",
    "type": "Push",
    "to": "device-token-abc123",
    "success": true,
    "timestamp": "2025-01-01T10:30:00Z",
    "errorMessage": null
  }
}
```

### **Get Supported Notification Types**

Retrieve all available notification types and their current status.

```bash
curl -X GET "https://your-app/api/notifications/types" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
[
  "Email",
  "Sms",
  "Push",
  "Webhook"
]
```

### **Health Check**

Check the overall health of the notification service.

```bash
curl -X GET "https://your-app/api/notifications/health"
```

**Response:**
```json
{
  "status": "healthy",
  "supportedTypes": ["Email", "Sms", "Push", "Webhook"],
  "timestamp": "2025-01-01T10:30:00Z"
}
```

## 📊 **Dashboard API**

### **Get Current Metrics**

Retrieve real-time performance metrics.

```bash
curl -X GET "https://your-app/api/dashboard/metrics/current" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "timestamp": "2025-01-01T10:30:00Z",
  "notificationsPerMinute": 45.2,
  "successRate": 98.5,
  "averageResponseTimeMs": 125.3,
  "activeConnections": 12,
  "activeStrategies": 4,
  "memoryUsageMB": 256.7,
  "cpuUsagePercent": 23.4,
  "strategyMetrics": {
    "Email": {
      "totalSent": 1234,
      "successRate": 99.2,
      "averageResponseTimeMs": 150.5,
      "isActive": true
    },
    "Sms": {
      "totalSent": 567,
      "successRate": 97.8,
      "averageResponseTimeMs": 200.2,
      "isActive": true
    },
    "Push": {
      "totalSent": 890,
      "successRate": 95.6,
      "averageResponseTimeMs": 180.8,
      "isActive": false
    },
    "Webhook": {
      "totalSent": 234,
      "successRate": 99.8,
      "averageResponseTimeMs": 95.2,
      "isActive": true
    }
  },
  "recentErrors": []
}
```

### **Get Strategy Metrics**

Get detailed metrics for a specific notification strategy.

```bash
curl -X GET "https://your-app/api/dashboard/metrics/strategy/Email" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "totalSent": 1234,
  "successRate": 99.2,
  "averageResponseTimeMs": 150.5,
  "isActive": true
}
```

### **Get All Strategy Metrics**

Retrieve metrics for all notification strategies.

```bash
curl -X GET "https://your-app/api/dashboard/metrics/strategies" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### **Get Active Connections**

View current SignalR connections to the dashboard.

```bash
curl -X GET "https://your-app/api/dashboard/connections" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
[
  {
    "connectionId": "abc123def456",
    "userId": "admin",
    "connectedAt": "2025-01-01T10:00:00Z",
    "groups": ["Dashboard"],
    "userAgent": "Mozilla/5.0...",
    "ipAddress": "192.168.1.100"
  }
]
```

### **Get Connection Count**

Get the total number of active dashboard connections.

```bash
curl -X GET "https://your-app/api/dashboard/connections/count" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "activeConnections": 12
}
```

### **Get System Health Overview**

Comprehensive health check including system metrics.

```bash
curl -X GET "https://your-app/api/dashboard/health" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### **Get Dashboard Summary**

Aggregated statistics for dashboard display.

```bash
curl -X GET "https://your-app/api/dashboard/summary" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "totalNotifications": 2935,
  "totalSuccessful": 2890,
  "totalFailed": 45,
  "overallSuccessRate": 98.5,
  "activeStrategies": 4,
  "systemLoad": {
    "cpuPercent": 23.4,
    "memoryMB": 256.7
  },
  "connections": {
    "active": 12,
    "peak": 25
  },
  "topPerformingStrategy": "Webhook",
  "alerts": "System healthy"
}
```

### **Reset Metrics** (Admin Only)

Reset all performance counters and metrics.

```bash
curl -X POST "https://your-app/api/dashboard/metrics/reset" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "message": "Metrics reset successfully"
}
```

### **Get Historical Metrics**

Retrieve metrics data for a specific time range.

```bash
curl -X GET "https://your-app/api/dashboard/metrics/history?startTime=2025-01-01T00:00:00Z&endTime=2025-01-01T23:59:59Z&intervalMinutes=15" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🎯 **Demo API**

### **Get Demo Scenarios**

List all available demo scenarios and their endpoints.

```bash
curl -X GET "https://your-app/api/demo/scenarios"
```

**Response:**
```json
{
  "title": "Notification Service - Comprehensive Demo Scenarios",
  "description": "Complete showcase of all implemented features across 6 categories",
  "totalScenarios": 12,
  "categories": 6,
  "scenarios": {
    "category1_DesignPatterns": {
      "strategy": {
        "endpoint": "/api/demo/patterns/strategy",
        "description": "Test all notification strategies"
      },
      "factory": {
        "endpoint": "/api/demo/patterns/factory",
        "description": "Demonstrate factory pattern"
      },
      "decorator": {
        "endpoint": "/api/demo/patterns/decorator",
        "description": "Show decorator chain execution"
      }
    },
    "category2_Performance": {
      "caching": {
        "endpoint": "/api/demo/performance/caching",
        "description": "Demonstrate Redis caching"
      },
      "async": {
        "endpoint": "/api/demo/performance/async",
        "description": "Show concurrent async processing"
      },
      "batching": {
        "endpoint": "/api/demo/performance/batching",
        "description": "Test message batching"
      }
    },
    "category4_Security": {
      "auth": {
        "endpoint": "/api/demo/security/auth",
        "description": "Show authentication & authorization"
      },
      "rateLimiting": {
        "endpoint": "/api/demo/security/rate-limiting",
        "description": "Test rate limiting"
      }
    },
    "category5_DevOps": {
      "health": {
        "endpoint": "/api/demo/devops/health",
        "description": "Comprehensive health checks"
      },
      "monitoring": {
        "endpoint": "/api/demo/devops/monitoring",
        "description": "Application monitoring"
      }
    },
    "category6_RealTime": {
      "dashboard": {
        "endpoint": "/api/demo/realtime/dashboard",
        "description": "Live metrics broadcasting"
      },
      "groups": {
        "endpoint": "/api/demo/realtime/groups",
        "description": "SignalR group management"
      }
    },
    "masterDemo": {
      "endpoint": "/api/demo/master-demo",
      "description": "Run all demos in sequence"
    }
  },
  "usage": {
    "authentication": "Most endpoints require authentication (JWT token)",
    "adminOnly": [
      "/api/demo/master-demo",
      "/api/demo/devops/monitoring"
    ],
    "anonymous": [
      "/api/demo/scenarios",
      "/api/demo/security/rate-limiting",
      "/api/demo/devops/health"
    ]
  }
}
```

### **Run Individual Demos**

Execute specific demo scenarios:

```bash
# Strategy Pattern Demo
curl -X POST "https://your-app/api/demo/patterns/strategy" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Caching Demo
curl -X POST "https://your-app/api/demo/performance/caching" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Health Check Demo
curl -X GET "https://your-app/api/demo/devops/health"
```

### **Master Demo** (Admin Only)

Execute all demo scenarios in sequence.

```bash
curl -X POST "https://your-app/api/demo/master-demo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "status": "Master Demo Completed",
  "totalExecutionTimeMs": 2450,
  "categoriesDemonstrated": 6,
  "totalDemos": 11,
  "results": {
    "StrategyPattern": "Completed",
    "FactoryPattern": "Completed",
    "DecoratorPattern": "Completed",
    "CachingDemo": "Completed",
    "AsyncDemo": "Completed",
    "BatchingDemo": "Completed",
    "SecurityDemo": "Completed",
    "HealthDemo": "Completed",
    "MonitoringDemo": "Completed",
    "RealtimeDemo": "Completed",
    "GroupsDemo": "Completed"
  },
  "summary": {
    "architecture": "Clean Architecture with CQRS patterns",
    "patterns": [
      "Strategy",
      "Factory",
      "Decorator",
      "Observer (SignalR)"
    ],
    "performance": [
      "Async processing",
      "Distributed caching",
      "Message batching"
    ],
    "security": [
      "JWT auth",
      "Role-based auth",
      "Rate limiting",
      "Audit logging"
    ],
    "devops": [
      "Health checks",
      "Monitoring",
      "CI/CD",
      "Docker",
      "Azure deployment"
    ],
    "realtime": [
      "SignalR hubs",
      "Live dashboard",
      "WebSocket communication"
    ]
  }
}
```

## 🌐 **Real-time Features**

### **SignalR Hub**

Connect to the real-time dashboard using SignalR:

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
    .withUrl('/notificationHub')
    .withAutomaticReconnect()
    .build();

connection.on('ReceiveMetrics', (metrics) => {
    console.log('New metrics received:', metrics);
});

connection.on('NotificationSent', (messageId) => {
    console.log('Notification sent:', messageId);
});

await connection.start();
await connection.invoke('JoinDashboard');
```

### **WebSocket Testing**

Test WebSocket connections for real-time features:

```bash
# Test SignalR negotiation
curl -I "https://your-app/notificationHub/negotiate"

# Test WebSocket upgrade
curl -N -H "Connection: Upgrade" \
     -H "Upgrade: websocket" \
     -H "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==" \
     -H "Sec-WebSocket-Version: 13" \
     "wss://your-app/notificationHub"
```

## 📋 **Rate Limiting**

The API implements rate limiting to prevent abuse:

- **General API**: 100 requests per minute
- **Authentication endpoints**: 10 requests per minute
- **Admin operations**: 50 requests per minute

When rate limited, you'll receive:
```json
{
  "status": 429,
  "title": "Too Many Requests",
  "detail": "Rate limit exceeded. Try again later."
}
```

## 🔍 **Error Handling**

### **Common Error Responses**

#### **400 Bad Request**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "To": ["The To field is required."],
    "Body": ["The Body field is required."]
  }
}
```

#### **401 Unauthorized**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication required"
}
```

#### **403 Forbidden**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Access denied"
}
```

#### **429 Too Many Requests**
```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Try again later.",
  "retryAfter": 60
}
```

#### **500 Internal Server Error**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred"
}
```

## 📊 **API Versioning**

The API supports versioning via headers:

```bash
# Specify API version
curl -H "api-version: 1.0" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     "https://your-app/api/notifications"
```

## 🧪 **Testing**

### **Postman Collection**

Import the provided Postman collection for comprehensive API testing:

```json
{
  "info": {
    "name": "Notification Service API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://your-app.azurewebsites.net"
    },
    {
      "key": "token",
      "value": ""
    }
  ]
}
```

### **Load Testing**

Run load tests to verify performance:

```bash
# Build and run load tests
cd LoadTests
dotnet build
dotnet run -- --base-url https://your-app --duration 300 --rate 100
```

## 📚 **SDKs and Libraries**

### **C# Client Library**

```csharp
using System.Net.Http.Json;

public class NotificationClient
{
    private readonly HttpClient _client;
    private string? _token;

    public NotificationClient(string baseUrl)
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task LoginAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username, password });

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _token = result?.Token;
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<NotificationResponse> SendNotificationAsync(
        string type, string to, string subject, string body)
    {
        var response = await _client.PostAsJsonAsync("/api/notifications",
            new { type, to, subject, body });

        return await response.Content.ReadFromJsonAsync<NotificationResponse>();
    }
}
```

## 📞 **Support**

### **Getting Help**

1. **API Documentation**: Visit `https://your-app/swagger` for interactive API docs
2. **Health Checks**: Use `/health` endpoint for service status
3. **Logs**: Check Azure Application Insights for detailed logs
4. **Issues**: Create GitHub issues for bugs or feature requests

### **Common Issues**

- **Token Expired**: Re-authenticate to get a new JWT token
- **Rate Limited**: Wait for the retry-after period specified in the response
- **Service Unavailable**: Check the health endpoint for system status
- **Validation Errors**: Review the error details and correct the request

---

**Version**: 1.0.0
**Last Updated**: January 2025
**Contact**: API Support Team
