using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration - try docker config first, fallback to local
builder.Configuration.AddJsonFile("ocelot.docker.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot
builder.Services.AddOcelot(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger for gateway documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "E-Commerce API Gateway",
        Version = "v1",
        Description = "API Gateway for E-Commerce Platform microservices"
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors();

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Gateway info endpoint
app.MapGet("/", () => Results.Ok(new
{
    name = "E-Commerce API Gateway",
    version = "1.0.0",
    services = new[]
    {
        new { name = "User Service", url = "/api/auth, /api/users", port = 5001 },
        new { name = "Product Service", url = "/api/products, /api/categories", port = 5002 },
        new { name = "Cart Service", url = "/api/cart", port = 5003 },
        new { name = "Order Service", url = "/api/orders", port = 5004 }
    },
    timestamp = DateTime.UtcNow
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        // Add links to individual service swagger docs
        c.InjectJavascript("/swagger-ui/custom.js");
    });
}

// Use Ocelot
await app.UseOcelot();

app.Run();
