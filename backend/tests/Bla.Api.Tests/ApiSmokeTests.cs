using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Bla.Api.Tests;

/// <summary>
/// Boots the real API via <see cref="WebApplicationFactory{TEntryPoint}"/> to prove the
/// cross-cutting wiring comes up: OpenAPI document is served and the public health endpoint is
/// reachable even though no PostgreSQL is available (it degrades rather than crashing the host).
/// </summary>
public class ApiSmokeTests : IClassFixture<ApiSmokeTests.TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ApiSmokeTests(TestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        // Pinned to OpenAPI 3.0.x.
        body.Should().Contain("\"openapi\": \"3.0");
    }

    [Fact]
    public async Task Health_IsPublic_AndDegradesWhenDbUnavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        // No auth challenge on the public endpoint...
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        // ...and with no DB reachable the readiness check reports unhealthy (503), not a crash.
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    public sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        // The API reads required config (connection string, JWT) during host construction,
        // before WebApplicationFactory's ConfigureAppConfiguration is merged. The environment-
        // variables provider is part of CreateBuilder's defaults, so set the values there to make
        // them visible to that early read. Double-underscore maps to nested config keys.
        public TestApiFactory()
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__Default",
                "Host=127.0.0.1;Port=1;Database=bla;Username=bla;Password=bla;Timeout=1;Command Timeout=1");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "bla-api-test");
            Environment.SetEnvironmentVariable("Jwt__Audience", "bla-spa-test");
            Environment.SetEnvironmentVariable(
                "Jwt__SigningKey", "test-only-signing-key-0123456789abcdef0123456789");
            Environment.SetEnvironmentVariable("Cors__AllowedOrigin", "http://localhost:4200");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }
}
