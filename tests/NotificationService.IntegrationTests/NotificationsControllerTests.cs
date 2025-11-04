using System.Net;
using System.Net.Http.Json;
using Core.Enums;
using Core.Models;
using Core.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace NotificationService.IntegrationTests;

public class NotificationsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public NotificationsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task SendNotification_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            Type = NotificationType.Email,
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            Metadata = new Dictionary<string, object>()
        };

        var expectedResult = TestDataBuilder.CreateSuccessResult();
        _factory.NotificationServiceMock.Setup(x => x.SendAsync(
                NotificationType.Email,
                It.Is<NotificationMessage>(m =>
                    m.To == request.To &&
                    m.Subject == request.Subject &&
                    m.Body == request.Body),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.MessageId.Should().Be(expectedResult.MessageId);
        result.SentAt.Should().Be(expectedResult.SentAt);
    }

    [Theory]
    [InlineData(null, "Subject", "Body", "To is required")]
    [InlineData("", "Subject", "Body", "To is required")]
    [InlineData("test@example.com", null, "Body", "Subject is required")]
    [InlineData("test@example.com", "", "Body", "Subject is required")]
    [InlineData("test@example.com", "Subject", null, "Body is required")]
    [InlineData("test@example.com", "Subject", "", "Body is required")]
    public async Task SendNotification_WithInvalidRequest_ReturnsBadRequest(
        string to, string subject, string body, string expectedError)
    {
        // Arrange
        var request = new
        {
            Type = NotificationType.Email,
            To = to,
            Subject = subject,
            Body = body,
            Metadata = new Dictionary<string, object>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain(expectedError);
    }

    [Fact]
    public async Task GetNotificationTypes_ReturnsAllTypes()
    {
        // Arrange
        var expectedTypes = new[] { NotificationType.Email, NotificationType.Sms };
        _factory.NotificationServiceMock.Setup(x => x.GetSupportedTypes())
            .Returns(Task.FromResult<IEnumerable<NotificationType>>(expectedTypes));

        // Act
        var response = await _client.GetAsync("/api/notifications/types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var types = await response.Content.ReadFromJsonAsync<NotificationType[]>();
        types.Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }
}

public class NotificationResponse
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
