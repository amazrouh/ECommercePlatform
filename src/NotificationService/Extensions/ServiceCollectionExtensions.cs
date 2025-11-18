using Core.Interfaces;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.EntityFrameworkCore;
using NotificationService.Caching;
using NotificationService.Configurations;
using NotificationService.Data;
using NotificationService.Decorators;
using NotificationService.Services;
using NotificationService.Strategies;
using NotificationService.Validators;

namespace NotificationService.Extensions;

/// <summary>
/// Extension methods for service collection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds notification services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add caching
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "NotificationService:";
        });
        services.AddScoped<ICacheService, TwoLevelCacheService>();

        // Add database
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Add strategies
        services.AddScoped<EmailNotificationStrategy>();
        services.AddScoped<SmsNotificationStrategy>();
        services.AddScoped<PushNotificationStrategy>();

        // Add factory and service
        services.AddScoped<INotificationStrategyFactory, NotificationStrategyFactory>();
        services.AddScoped<INotificationService, Services.NotificationService>();

        // Add validators
        services.AddScoped<EmailMessageValidator>();
        services.AddScoped<SmsMessageValidator>();
        services.AddScoped<PushMessageValidator>();

        // Add decorators
        services.Decorate<INotificationService, LoggingNotificationDecorator>();
        services.Decorate<INotificationService, RetryNotificationDecorator>();
        services.Decorate<INotificationService, CircuitBreakerNotificationDecorator>();
        services.Decorate<INotificationService, CachingNotificationDecorator>();

        // Add configuration
        services.Configure<EmailConfig>(configuration.GetSection("Email"));
        services.Configure<SmsConfig>(configuration.GetSection("Sms"));
        services.Configure<PushConfig>(configuration.GetSection("Push"));
        services.Configure<JwtConfig>(configuration.GetSection("Jwt"));
        services.Configure<SecurityConfig>(configuration.GetSection("Security"));

        // Add security services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddHttpContextAccessor();

        // Add dashboard services
        services.AddSingleton<MessageBatchingService>();
        services.AddSingleton<DashboardMetricsService>();
        services.AddSingleton<Core.Interfaces.IMetricsRecorder>(sp => sp.GetRequiredService<DashboardMetricsService>());
        services.AddHostedService(sp => sp.GetRequiredService<DashboardMetricsService>());

        return services;
    }
}
