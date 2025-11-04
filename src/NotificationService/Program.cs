using Core.Interfaces;
using Microsoft.OpenApi.Models;
using NotificationService.Decorators;
using Swashbuckle.AspNetCore.Filters;
using NotificationService.Extensions;
using NotificationService.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notification Service API",
        Version = "v1",
        Description = "API for sending various types of notifications",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add request/response examples
    options.ExampleFilters();
});

// Add notification services with decorators
builder.Services.AddNotificationServices(builder.Configuration);
builder.Services.Decorate<INotificationService, LoggingNotificationDecorator>();
builder.Services.Decorate<INotificationService, RetryNotificationDecorator>();
builder.Services.Decorate<INotificationService, CircuitBreakerNotificationDecorator>();

// Add AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
