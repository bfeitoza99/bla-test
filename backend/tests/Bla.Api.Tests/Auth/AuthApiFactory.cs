using Bla.Application.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bla.Api.Tests.Auth;

/// <summary>
/// Boots the real API but swaps the Npgsql <see cref="IUserRepository"/> for an in-memory fake,
/// so the auth endpoint tests exercise the full pipeline (routing, auth, validation, the real
/// hasher and JWT service) deterministically and without a database. The fake is a singleton so
/// state survives across requests within a test.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public AuthApiFactory()
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

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        });
    }
}
