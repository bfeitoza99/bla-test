using Bla.Api.Tests.Auth;
using Bla.Application.Tasks;
using Bla.Application.Users;
using Bla.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bla.Api.Tests.Tasks;

/// <summary>
/// Boots the real API but swaps the Npgsql <see cref="ITaskRepository"/> (and
/// <see cref="IUserRepository"/>) for in-memory fakes, so the task endpoint tests exercise the full
/// pipeline (routing, JWT auth, validation, the real token service) deterministically and without a
/// database. The fakes are singletons so state survives across requests within a test. Authenticated
/// requests use a <em>real</em> JWT minted by the registered <see cref="ITokenService"/> for a test
/// user — see <see cref="CreateAuthenticatedClient"/>.
/// </summary>
public sealed class TasksApiFactory : WebApplicationFactory<Program>
{
    public TasksApiFactory()
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
            services.RemoveAll<ITaskRepository>();
            services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();

            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        });
    }

    /// <summary>
    /// Creates an HTTP client whose every request carries a valid Bearer token for a freshly-created
    /// test user, returning that user's id so tests can assert ownership behavior. The token is
    /// minted by the real <see cref="ITokenService"/> so it passes the API's JWT validation exactly.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(out Guid userId)
    {
        var id = Guid.NewGuid();
        userId = id;

        using var scope = Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        // A minimal valid user; the token's subject claim carries the id the controller reads back.
        var user = User.Restore(id, $"user-{id:N}@bla.local", "unused-hash", DateTime.UtcNow);
        var (token, _) = tokenService.CreateToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
