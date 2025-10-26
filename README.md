# ECommerce Platform

A modern e-commerce platform built with .NET 8, implementing a microservices architecture.

## Project Structure

- `src/Core/`: Core domain models, interfaces, and shared utilities
- `src/NotificationService/`: Microservice handling notification delivery (Email, SMS, Push notifications)
- `tests/NotificationService.Tests/`: Unit tests for the Notification Service

## Technology Stack

- .NET 8.0
- ASP.NET Core Web API
- xUnit for testing

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code

### Building the Solution

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Project Status

This project is currently in development.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
