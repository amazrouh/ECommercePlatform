using Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NotificationService.Configurations;
using NotificationService.Decorators;
using NotificationService.Extensions;
using NotificationService.Hubs;
using NotificationService.Middleware;
using NotificationService.Security;
using NotificationService.Services;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT authentication
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig?.Issuer,
        ValidAudience = jwtConfig?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig?.Secret ?? throw new InvalidOperationException("JWT Secret is not configured")))
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = async context =>
        {
            // Log authentication failure
            var auditLogger = context.HttpContext.RequestServices.GetRequiredService<IAuditLogger>();
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await auditLogger.LogAuthenticationFailureAsync("unknown", ipAddress, $"JWT validation failed: {context.Exception.Message}");
        }
    };
});

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUser", policy => policy.RequireRole("User", "Admin"));
});

// Add rate limiting
builder.Services.AddCustomRateLimiting(builder.Configuration);

// Configure SignalR
var signalRBuilder = builder.Services.AddSignalR(options =>
{
    // Configure SignalR options for performance
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 102400; // 100KB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// Configure Azure SignalR Service if enabled
var azureSignalRConfig = builder.Configuration.GetSection("AzureSignalR");
if (azureSignalRConfig.GetValue<bool>("Enabled"))
{
    signalRBuilder.AddAzureSignalR(options =>
    {
        options.ConnectionString = azureSignalRConfig["ConnectionString"];
        options.ServerStickyMode = ServerStickyMode.Required; // Enable sticky sessions for better performance
    });
}

// Configure MessagePack for SignalR
MessagePackConfig.ConfigureMessagePack();

// Configure Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notification Service API",
        Version = "1.0.0",
        Description = "API for sending various types of notifications",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });

    // Configure enums and other settings


    // Ensure proper OpenAPI version specification
    options.DescribeAllParametersInCamelCase();


    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add dashboard services
builder.Services.AddSingleton<MessageBatchingService>();
builder.Services.AddHostedService<DashboardMetricsService>();

// Add notification services with decorators
builder.Services.AddNotificationServices(builder.Configuration);
builder.Services.Decorate<INotificationService, LoggingNotificationDecorator>();
builder.Services.Decorate<INotificationService, RetryNotificationDecorator>();
builder.Services.Decorate<INotificationService, CircuitBreakerNotificationDecorator>();

// Add health checks
builder.Services.AddHealthChecks();

// Add AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for testing
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
});

// Enable HTTPS redirection and HSTS
app.UseHttpsRedirection();
app.UseHsts();

// Enable rate limiting
app.UseRateLimiter();

// Add security headers
app.UseSecurityHeaders();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Enable static files for dashboard
app.UseStaticFiles();

// Map SignalR hubs
app.MapHub<NotificationHub>("/notificationHub");

// Add custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

// Map dashboard route
app.MapGet("/dashboard", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/dashboard.html");
});

app.Run();

public partial class Program { }
