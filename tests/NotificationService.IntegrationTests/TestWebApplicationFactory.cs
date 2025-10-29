using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace NotificationService.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Mock<INotificationService> _notificationServiceMock;

    public TestWebApplicationFactory()
    {
        _notificationServiceMock = new Mock<INotificationService>();
    }

    public Mock<INotificationService> NotificationServiceMock => _notificationServiceMock;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real notification service
            services.RemoveAll<INotificationService>();

            // Add the mock notification service
            services.AddScoped(_ => _notificationServiceMock.Object);
        });
    }
}
