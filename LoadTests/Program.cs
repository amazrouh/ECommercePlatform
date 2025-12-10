using NBomber.CSharp;
using Serilog;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace LoadTests;

/// <summary>
/// Comprehensive load testing suite for Notification Service
/// Tests high-volume notifications, concurrent dashboard users, and performance benchmarks
/// </summary>
public class Program
{
    private static string _baseUrl = "http://localhost:8080";
    private static string _jwtToken = "";
    private static DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private static readonly SemaphoreSlim _authSemaphore = new SemaphoreSlim(1, 1);

    public static void Main(string[] args)
    {
        Console.WriteLine($"Args: {string.Join(", ", args)}");
        if (args.Length > 0 && args[0] == "simple-test")
        {
            Console.WriteLine("Running simple test mode");
            // Simple test mode - just make one request
            SimpleTestAsync().GetAwaiter().GetResult();
            return;
        }

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
        await _authSemaphore.WaitAsync();
        try
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
                // Set token expiry to 5 minutes from now (be conservative)
                _tokenExpiry = DateTimeOffset.UtcNow.AddMinutes(5);
                Console.WriteLine("✅ Authentication successful");
                if (!string.IsNullOrEmpty(_jwtToken))
                {
                    Console.WriteLine($"Token preview: {_jwtToken.Substring(0, Math.Min(20, _jwtToken.Length))}...");
                    Console.WriteLine($"Token expires: {_tokenExpiry}");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"⚠️ Authentication failed: {response.StatusCode} - {errorContent}");
            }
        }
        finally
        {
            _authSemaphore.Release();
        }
    }

    private static async Task SimpleTestAsync()
    {
        Console.WriteLine("🔬 Running Simple Test - using PowerShell to make the request");

        try
        {
            // Use PowerShell to execute the exact same commands that work
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"$token = (Invoke-WebRequest -Uri '{_baseUrl}/api/auth/login' -Method POST -Headers @{{'Content-Type'='application/json'}} -Body '{{\\\"username\\\":\\\"admin\\\",\\\"password\\\":\\\"admin123\\\"}}' | Select-Object -ExpandProperty Content | ConvertFrom-Json).token; Write-Host \\\"Got token\\\"; Invoke-WebRequest -Uri '{_baseUrl}/api/notifications' -Method POST -Headers @{{'Authorization'=\\\"Bearer $token\\\"; 'Content-Type'='application/json'}} -Body '{{\\\"type\\\":1,\\\"to\\\":\\\"test@example.com\\\",\\\"subject\\\":\\\"Test\\\",\\\"body\\\":\\\"Test\\\"}}' | Select-Object StatusCode, Content\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine("❌ Failed to start PowerShell process");
                return;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            Console.WriteLine($"PowerShell output: {output}");
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"PowerShell error: {error}");
            }

            if (process.ExitCode == 0)
            {
                Console.WriteLine("✅ PowerShell request succeeded");

                // Check dashboard metrics
                using var metricsClient = new HttpClient();
                var metricsResponse = await metricsClient.GetAsync($"{_baseUrl}/api/dashboard/metrics/current");
                if (metricsResponse.IsSuccessStatusCode)
                {
                    var metricsContent = await metricsResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Dashboard metrics: {metricsContent}");
                }
            }
            else
            {
                Console.WriteLine($"❌ PowerShell request failed with exit code {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static async Task EnsureValidTokenAsync()
    {
        if (string.IsNullOrEmpty(_jwtToken) || DateTimeOffset.UtcNow >= _tokenExpiry)
        {
            Console.WriteLine("🔄 Token expired or missing, re-authenticating...");
            await _authSemaphore.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (string.IsNullOrEmpty(_jwtToken) || DateTimeOffset.UtcNow >= _tokenExpiry)
                {
                    await AuthenticateAsync();
                }
            }
            finally
            {
                _authSemaphore.Release();
            }
        }
    }

    private static void RunNotificationLoadTests()
    {
        Console.WriteLine("📧 Running Notification Load Tests...");

        // High-volume email notifications
        var emailScenario = Scenario.Create("email_notifications", async context =>
        {
            var emailNumber = context.ScenarioInfo.ThreadNumber + context.InvocationNumber;

            try
            {
                using var client = new HttpClient();

                var requestBody = new
                {
                    type = 1, // Email
                    to = $"loadtest-{emailNumber}@example.com",
                    subject = $"Load Test Email {emailNumber}",
                    body = $"This is a load test notification #{emailNumber} sent at {DateTime.UtcNow}"
                };

                // Authenticate for every request to ensure fresh token
                string freshToken;
                using (var authClient = new HttpClient())
                {
                    var authResponse = await authClient.PostAsync($"{_baseUrl}/api/auth/login",
                        new StringContent("{\"username\":\"admin\",\"password\":\"admin123\"}", System.Text.Encoding.UTF8, "application/json"));
                    if (authResponse.IsSuccessStatusCode)
                    {
                        var authContent = await authResponse.Content.ReadAsStringAsync();
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(authContent);
                        freshToken = loginResponse?.Token ?? "";
                    }
                    else
                    {
                        Console.WriteLine($"Auth failed: {authResponse.StatusCode}");
                        return Response.Fail();
                    }
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
                Console.WriteLine($"[DEBUG] Using token for request {emailNumber}: {freshToken?.Substring(0, Math.Min(30, freshToken?.Length ?? 0))}...");

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/notifications", requestBody);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✓ Request {emailNumber} succeeded");
                    return Response.Ok();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✗ Request {emailNumber} failed with status {response.StatusCode}: {errorContent}");
                    Console.WriteLine($"Request URL: {response.RequestMessage?.RequestUri}");
                    Console.WriteLine($"Auth header: {client.DefaultRequestHeaders.Authorization}");

                    return Response.Fail();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Request {emailNumber} exception: {ex.Message}");
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(2), during: TimeSpan.FromSeconds(10))  // 1 request every 2 seconds for 10 seconds
        )
        .WithoutWarmUp();

        // SMS notifications
        var smsScenario = Scenario.Create("sms_notifications", async context =>
        {
            try
            {
                using var client = new HttpClient();

                // Authenticate for every request to ensure fresh token
                string freshToken;
                using (var authClient = new HttpClient())
                {
                    var authResponse = await authClient.PostAsync($"{_baseUrl}/api/auth/login",
                        new StringContent("{\"username\":\"admin\",\"password\":\"admin123\"}", System.Text.Encoding.UTF8, "application/json"));
                    if (authResponse.IsSuccessStatusCode)
                    {
                        var authContent = await authResponse.Content.ReadAsStringAsync();
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(authContent);
                        freshToken = loginResponse?.Token ?? "";
                    }
                    else
                    {
                        Console.WriteLine($"Auth failed: {authResponse.StatusCode}");
                        return Response.Fail();
                    }
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
                var smsNumber = context.ScenarioInfo.ThreadNumber + context.InvocationNumber;

                var requestBody = new
                {
                    type = 2, // SMS
                    to = $"+1{new Random().Next(100000000, 999999999)}",
                    subject = "",
                    body = $"Load test SMS #{smsNumber}"
                };

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

                // Authenticate for every request to ensure fresh token
                string freshToken;
                using (var authClient = new HttpClient())
                {
                    var authResponse = await authClient.PostAsync($"{_baseUrl}/api/auth/login",
                        new StringContent("{\"username\":\"admin\",\"password\":\"admin123\"}", System.Text.Encoding.UTF8, "application/json"));
                    if (authResponse.IsSuccessStatusCode)
                    {
                        var authContent = await authResponse.Content.ReadAsStringAsync();
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(authContent);
                        freshToken = loginResponse?.Token ?? "";
                    }
                    else
                    {
                        Console.WriteLine($"Auth failed: {authResponse.StatusCode}");
                        return Response.Fail();
                    }
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
                var pushNumber = context.ScenarioInfo.ThreadNumber + context.InvocationNumber;

                var requestBody = new
                {
                    type = 3, // Push
                    to = $"device-token-{pushNumber}-{Guid.NewGuid()}",
                    subject = "Load Test Push",
                    body = $"Push notification #{pushNumber}"
                };

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
            .RegisterScenarios(emailScenario)  // Just test email scenario first
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
