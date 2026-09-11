using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;
using TrainingManagement.Api.Common.Extensions;

namespace TrainingManagement.Api.ModuleTests;

internal static class HttpAuthorizationTests
{
    // Exercises the real HTTP authentication pipeline without connecting to shared Oracle.
    public static async Task RunAsync()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "src", "backend")))
            root = root.Parent;
        if (root is null)
            throw new InvalidOperationException("Run this test from the repository build output.");

        var apiDirectory = Path.Combine(root.FullName, "src", "backend", "TrainingManagement.Api");
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var password = Guid.NewGuid().ToString("N");
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = apiDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[]
        {
            Path.Combine(AppContext.BaseDirectory, "TrainingManagement.Api.dll"),
            $"--urls=http://127.0.0.1:{port}",
            "--ConnectionStrings:OracleDb=",
            "--Auth:EnableLocalDemoUsers=true",
            $"--Auth:LocalDemoPassword={password}",
            $"--Jwt:SigningKey={Guid.NewGuid():N}{Guid.NewGuid():N}",
            "--Logging:EventLog:LogLevel:Default=None"
        }) start.ArgumentList.Add(argument);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["DOTNET_ENVIRONMENT"] = "Development";

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start the API.");
        // Drain both streams so the child cannot block on a full output buffer.
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        try
        {
            var ready = false;
            for (var attempt = 0; attempt < 60 && !process.HasExited; attempt++)
            {
                try
                {
                    using var health = await client.GetAsync("/api/health");
                    if (health.IsSuccessStatusCode) { ready = true; break; }
                }
                catch (HttpRequestException) { }
                await Task.Delay(250);
            }
            TestAssert.True(ready, "Temporary API did not start within 15 seconds.");

            var endpoints = new[]
            {
                ("GET", "/api/courses", 200, 200),
                ("GET", "/api/courses/-1", 404, 404),
                ("POST", "/api/courses", 400, 403),
                ("PUT", "/api/courses/-1", 400, 403),
                ("PATCH", "/api/courses/-1/publish", 404, 403),
                ("PATCH", "/api/courses/-1/close", 404, 403),
                ("GET", "/api/trainers", 200, 403),
                ("GET", "/api/trainers/-1", 404, 403),
                ("POST", "/api/trainers", 400, 403),
                ("PUT", "/api/trainers/-1", 400, 403),
                // 成绩录入:仅 HR/ADMIN 通过授权(空体走到校验返回 400),普通员工与主管 403。
                ("POST", "/api/tests", 400, 403)
            };
            foreach (var token in new string?[] { null, "invalid-token" })
                foreach (var endpoint in endpoints)
                    await CheckStatus(endpoint.Item1, endpoint.Item2, token, 401);

            foreach (var user in new[]
            {
                ("admin@example.com", 54L, "ADMIN", true),
                ("employee@example.com", 55L, "EMPLOYEE", false),
                ("manager@example.com", 56L, "DEPT_MANAGER", false),
                ("hr@example.com", 57L, "HR", true)
            })
            {
                using var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = user.Item1, password });
                TestAssert.Equal(HttpStatusCode.OK, login.StatusCode, $"Login for {user.Item3}.");
                using var document = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
                var data = document.RootElement.GetProperty("data");
                TestAssert.Equal(user.Item2, data.GetProperty("user").GetProperty("empId").GetInt64(), "Login employee ID.");
                var token = data.GetProperty("accessToken").GetString()!;
                var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToArray();
                TestAssert.True(claims.Any(c => c.Type == "emp_id" && c.Value == user.Item2.ToString()), "JWT emp_id.");
                TestAssert.True(claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Item2.ToString()), "JWT NameIdentifier.");
                TestAssert.True(claims.Any(c => c.Type == ClaimTypes.Role && c.Value == user.Item3), "JWT role.");
                using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
                meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var me = await client.SendAsync(meRequest);
                TestAssert.Equal(HttpStatusCode.OK, me.StatusCode, "Current-user endpoint.");
                using var meDocument = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
                TestAssert.Equal(user.Item2, meDocument.RootElement.GetProperty("data").GetProperty("empId").GetInt64(), "Current-user employee ID.");
                foreach (var endpoint in endpoints)
                    await CheckStatus(endpoint.Item1, endpoint.Item2, token, user.Item4 ? endpoint.Item3 : endpoint.Item4);
                if (user.Item4)
                {
                    foreach (var query in new[] { "page=0", "pageSize=0", "pageSize=101" })
                        await CheckStatus("GET", "/api/trainers?" + query, token, 400);
                    using var pagedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/trainers?page=2&pageSize=7");
                    pagedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var paged = await client.SendAsync(pagedRequest);
                    TestAssert.Equal(HttpStatusCode.OK, paged.StatusCode, "Trainer paged HTTP response.");
                    using var pageDocument = JsonDocument.Parse(await paged.Content.ReadAsStringAsync());
                    var page = pageDocument.RootElement.GetProperty("data");
                    TestAssert.Equal(2, page.GetProperty("page").GetInt32(), "HTTP page metadata.");
                    TestAssert.Equal(7, page.GetProperty("pageSize").GetInt32(), "HTTP page size metadata.");
                    TestAssert.Equal(0L, page.GetProperty("total").GetInt64(), "Unconfigured database has no rows; this is not an Oracle test.");
                    TestAssert.Equal(JsonValueKind.Array, page.GetProperty("items").ValueKind, "Trainer list uses data.items.");
                }
                Console.WriteLine($"PASS HTTP {user.Item3}: login, ID {user.Item2}, JWT, /me, {endpoints.Length} endpoints");
            }

            foreach (var claimType in new[] { "emp_id", ClaimTypes.NameIdentifier })
            {
                var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(claimType, "9876") }));
                TestAssert.Equal<long?>(9876, principal.GetEmployeeId(), "Read employee ID from either supported claim.");
            }
            Console.WriteLine(
                $"PASS HTTP no token / invalid token: {2 * endpoints.Length} checks; role matrix: {4 * endpoints.Length} checks");
            using var swagger = await client.GetAsync("/swagger/v1/swagger.json");
            TestAssert.Equal(HttpStatusCode.OK, swagger.StatusCode, "OpenAPI generation.");
            using var swaggerDocument = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
            var schemas = swaggerDocument.RootElement.GetProperty("components").GetProperty("schemas");
            TestAssert.True(schemas.GetProperty("CloseCourseResponse").GetProperty("properties").TryGetProperty("closed", out _), "Typed close schema.");
            TestAssert.True(schemas.GetProperty("PublishCourseResponse").GetProperty("properties").TryGetProperty("published", out _), "Typed publish schema.");
            Console.WriteLine("PASS HTTP trainer pagination: 8 checks; typed action OpenAPI schemas");
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await Task.WhenAll(output, errors);
        }

        async Task CheckStatus(string method, string path, string? token, int expected)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // Invalid bodies prove authorized writes reach validation, without performing a write.
            if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
            using var response = await client.SendAsync(request);
            TestAssert.Equal(expected, (int)response.StatusCode, $"{method} {path} authorization.");
        }
    }
}
