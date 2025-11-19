using NBomber.CSharp;
using Serilog;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Net.Http;

namespace LoadTests;

/// <summary>
/// Comprehensive load testing suite for Notification Service
/// Tests high-volume notifications, concurrent dashboard users, and performance benchmarks
/// </summary>
public class Program
{
    private static string _baseUrl = "http://localhost:8080";
    private static string _jwtToken = "";

    public static void Main(string[] args)
    {
        // Configure Serilog for detailed logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("loadtest-results-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _baseUrl = configuration["TestSettings:BaseUrl"] ?? "http://localhost:8080";

        Console.WriteLine("🚀 Notification Service Load Testing Suite");
        Console.WriteLine("===========================================");
        Console.WriteLine($"Target URL: {_baseUrl}");
        Console.WriteLine();

        // Authenticate first to get JWT token
        AuthenticateAsync().GetAwaiter().GetResult();

        // Run comprehensive load tests
        RunNotificationLoadTests();
        RunDashboardLoadTests();
        RunHealthCheckLoadTests();
        RunSecurityLoadTests();

        Console.WriteLine("\n✅ Load testing completed. Check loadtest-results-.txt for detailed results.");
    }

    private static async Task AuthenticateAsync()
    {
        using var client = new HttpClient();
        var loginRequest = new
        {
            username = "admin",
            password = "admin123"
        };

        var content = new StringContent(JsonSerializer.Serialize(loginRequest), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{_baseUrl}/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
            _jwtToken = loginResponse?.Token ?? "";
            Console.WriteLine("✅ Authentication successful");
        }
        else
        {
            Console.WriteLine("⚠️ Authentication failed, proceeding with anonymous tests only");
        }
    }

    private static void RunNotificationLoadTests()
    {
        Console.WriteLine("📧 Running Notification Load Tests...");

        // High-volume email notifications
        var emailScenario = Scenario.Create("email_notifications", async context =>
        {
            try
            {
                using var client = new HttpClient();
                var emailNumber = context.ScenarioInfo.ThreadNumber + context.InvocationNumber;

                var requestBody = new
                {
                    type = 1, // Email
                    to = $"loadtest-{emailNumber}@example.com",
                    subject = $"Load Test Email {emailNumber}",
                    body = $"This is a load test notification #{emailNumber} sent at {DateTime.UtcNow}"
                };

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/notifications", requestBody);

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );

        // SMS notifications
        var smsScenario = Scenario.Create("sms_notifications", async context =>
        {
            try
            {
                using var client = new HttpClient();
                var smsNumber = context.ScenarioInfo.ThreadNumber + context.InvocationNumber;

                var requestBody = new
                {
                    type = 2, // SMS
                    to = $"+1{new Random().Next(100000000, 999999999)}",
                    subject = "",
                    body = $"Load test SMS #{smsNumber}"
                };

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/notifications", requestBody);

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        // Push notifications
        var pushScenario = Scenario.Create("push_notifications", async context =>
        {
            try
            {
                using var client = new HttpClient();
                var pushNumber = context.ScenarioInfo.ThreadNumber + context.InvocationNumber;

                var requestBody = new
                {
                    type = 3, // Push
                    to = $"device-token-{pushNumber}-{Guid.NewGuid()}",
                    subject = "Load Test Push",
                    body = $"Push notification #{pushNumber}"
                };

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/notifications", requestBody);

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(emailScenario, smsScenario, pushScenario)
            .WithTestSuite("Notification Load Tests")
            .WithTestName("Notification Performance Benchmark")
            .Run();
    }

    private static void RunDashboardLoadTests()
    {
        Console.WriteLine("📊 Running Dashboard Load Tests...");

        // Dashboard metrics polling
        var dashboardScenario = Scenario.Create("dashboard_metrics", async context =>
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                var response = await client.GetAsync($"{_baseUrl}/api/dashboard/metrics/current");

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );

        // Health check polling
        var healthScenario = Scenario.Create("health_checks", async context =>
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{_baseUrl}/api/notifications/health");

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(3))
        );

        NBomberRunner
            .RegisterScenarios(dashboardScenario, healthScenario)
            .WithTestSuite("Dashboard Load Tests")
            .WithTestName("Dashboard Performance Benchmark")
            .Run();
    }

    private static void RunHealthCheckLoadTests()
    {
        Console.WriteLine("🏥 Running Health Check Load Tests...");

        // Comprehensive health checks
        var healthScenario = Scenario.Create("comprehensive_health", async context =>
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{_baseUrl}/api/demo/devops/health");

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(2))
        );

        // System metrics endpoint
        var metricsScenario = Scenario.Create("system_metrics", async context =>
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                var response = await client.GetAsync($"{_baseUrl}/api/dashboard/metrics/current");

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 15, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(healthScenario, metricsScenario)
            .WithTestSuite("Health Check Load Tests")
            .WithTestName("Health Monitoring Performance")
            .Run();
    }

    private static void RunSecurityLoadTests()
    {
        Console.WriteLine("🔒 Running Security Load Tests...");

        // Rate limiting test
        var rateLimitScenario = Scenario.Create("rate_limiting", async context =>
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{_baseUrl}/api/demo/security/rate-limiting");

                // Both 200 (allowed) and 429 (rate limited) are acceptable
                return (response.StatusCode == System.Net.HttpStatusCode.OK ||
                       response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        // Authentication load test
        var authScenario = Scenario.Create("authentication_load", async context =>
        {
            try
            {
                using var client = new HttpClient();

                var loginRequest = new
                {
                    username = "admin",
                    password = "admin123"
                };

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/auth/login", loginRequest);

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        // Unauthorized access attempts
        var unauthorizedScenario = Scenario.Create("unauthorized_access", async context =>
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{_baseUrl}/api/dashboard/metrics/current");

                // 401 Unauthorized is expected
                return response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? Response.Ok()
                    : Response.Fail();
            }
            catch
            {
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(rateLimitScenario, authScenario, unauthorizedScenario)
            .WithTestSuite("Security Load Tests")
            .WithTestName("Security Performance Benchmark")
            .Run();
    }

    private class LoginResponse
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
