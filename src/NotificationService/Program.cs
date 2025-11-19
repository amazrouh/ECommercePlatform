using Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
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
        Description = "Comprehensive notification service with Email, SMS, Push, and Webhook support",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });

    // Configure enums and other settings
    options.DescribeAllParametersInCamelCase();

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add request examples
    options.ExampleFilters();

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                        Enter 'Bearer' [space] and then your token in the text input below.
                        Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
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

// Add Swagger examples
builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetExecutingAssembly());

// Add notification services with decorators
builder.Services.AddNotificationServices(builder.Configuration);

// Configure Azure App Configuration (only if enabled)
var azureAppConfig = builder.Configuration.GetSection("AzureAppConfig").Get<AzureAppConfig>();
if (azureAppConfig?.Enabled == true && !string.IsNullOrEmpty(azureAppConfig.ConnectionString))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(azureAppConfig.ConnectionString)
               .UseFeatureFlags(featureFlagOptions =>
               {
                   featureFlagOptions.CacheExpirationInterval = azureAppConfig.RefreshInterval;
                   featureFlagOptions.Label = "NotificationService";
               })
               .Select(KeyFilter.Any, LabelFilter.Null)
               .Select(KeyFilter.Any, "NotificationService")
               .ConfigureRefresh(refreshOptions =>
               {
                   refreshOptions.Register(azureAppConfig.SentinelKey, refreshAll: true)
                               .SetCacheExpiration(azureAppConfig.RefreshInterval);
               });

        if (azureAppConfig.UseKeyVault)
        {
            // Key Vault integration is handled through configuration references
            // The managed identity is already configured at the service level
        }
    });
}

// Add Azure App Configuration middleware for configuration refresh
if (azureAppConfig?.Enabled == true)
{
    builder.Services.AddAzureAppConfiguration();
}

// Add feature management
builder.Services.AddFeatureManagement();

// Add health checks
builder.Services.AddHealthChecks();

// Add AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Configure the HTTP request pipeline
// Use Azure App Configuration middleware (if enabled) - must be very early in pipeline
if (azureAppConfig?.Enabled == true)
{
    app.UseAzureAppConfiguration();
}

// Enable Swagger in all environments for testing
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    options.DefaultModelsExpandDepth(-1); // Hide schemas section
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
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

// Debug route to test SignalR
app.MapGet("/debug", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>Debug Page</title>
    <script src='/js/signalr/signalr.min.js'></script>
</head>
<body>
    <h1>SignalR Debug Test</h1>
    <div id='status'>Initializing...</div>
    <div id='messages'></div>
    <button onclick='testConnection()'>Test Connection</button>
    <script>
        let connection;
        const statusDiv = document.getElementById('status');
        const messagesDiv = document.getElementById('messages');

        function log(message) {
            console.log('DEBUG:', message);
            messagesDiv.innerHTML += '<div>' + message + '</div>';
        }

        function testConnection() {
            try {
                log('Starting testConnection()...');
                statusDiv.textContent = 'Testing SignalR...';

                // Check if signalR is available
                if (typeof signalR === 'undefined') {
                    log('ERROR: signalR is undefined!');
                    statusDiv.textContent = 'SignalR not loaded';
                    return;
                }

                log('SignalR available: ' + (typeof signalR !== 'undefined'));
                log('HubConnectionBuilder: ' + (typeof signalR.HubConnectionBuilder));

                connection = new signalR.HubConnectionBuilder()
                    .withUrl('/notificationHub')
                    .withAutomaticReconnect()
                    .build();

                log('Connection object created');

                connection.on('ReceiveMetrics', (metrics) => {
                    log('Received metrics: ' + JSON.stringify(metrics, null, 2));
                });

                connection.onclose((error) => {
                    log('Connection closed: ' + (error ? error.message : 'No error'));
                });

                log('Starting connection...');
                connection.start()
                    .then(() => {
                        log('Connection.start() resolved');
                        statusDiv.textContent = 'Connected!';
                        log('Connection started successfully');
                        return connection.invoke('JoinDashboard');
                    })
                    .then(() => {
                        log('Joined dashboard group');
                    })
                    .catch(err => {
                        log('Connection error: ' + err.message);
                        log('Error type: ' + typeof err);
                        log('Error stack: ' + (err.stack || 'No stack'));
                        statusDiv.textContent = 'Connection failed: ' + err.message;
                    });
            } catch (globalError) {
                log('Global error in testConnection: ' + globalError.message);
                statusDiv.textContent = 'Error: ' + globalError.message;
            }
        }

        // Auto-test on load
        window.onload = function() {
            log('Window loaded, starting test in 1 second...');
            setTimeout(testConnection, 1000);
        };

        // Manual test button
        log('Page loaded successfully');
    </script>
</body>
</html>");
});

app.Run();

public partial class Program { }
