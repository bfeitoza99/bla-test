using Bla.Application.Tasks;
using Bla.Application.Users;
using Bla.Infrastructure.Migrations;
using Bla.Infrastructure.Security;
using Bla.Infrastructure.Seeding;
using Bla.Infrastructure.Tasks;
using Bla.Infrastructure.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bla.Infrastructure;

/// <summary>
/// Composition seam for the Infrastructure layer.
/// Feature agents register their Npgsql repository implementations (against Application ports),
/// the password hasher, and the JWT token service here. The foundation wires only the
/// cross-cutting pieces it owns: the SQL migration runner.
/// </summary>
/// <remarks>
/// The <see cref="Npgsql.NpgsqlDataSource"/> itself is registered by the API composition root
/// (from <c>ConnectionStrings:Default</c>) so that the data source and the health check share a
/// single instance. This method assumes that registration has already happened.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure-layer services. Call once from the API composition root,
    /// after the <see cref="Npgsql.NpgsqlDataSource"/> has been registered.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SQL migration runner (foundation-owned). Path is overridable via config for tests/containers.
        services.AddSingleton(sp =>
        {
            var options = new MigrationOptions();
            var configured = configuration["Migrations:Path"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                options.MigrationsPath = configured;
            }

            return options;
        });
        services.AddSingleton<MigrationRunner>();

        // Auth slice — repositories + security primitives behind their Application ports.
        services.AddScoped<IUserRepository, NpgsqlUserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Tasks slice — repository behind its Application port.
        services.AddScoped<ITaskRepository, NpgsqlTaskRepository>();

        // JWT issuing options, bound from the shared 'Jwt' section (same values the API validates).
        services.AddSingleton(sp =>
        {
            var options = configuration.GetSection(JwtTokenOptions.SectionName).Get<JwtTokenOptions>()
                ?? throw new InvalidOperationException(
                    "Missing required configuration section 'Jwt'.");

            if (string.IsNullOrWhiteSpace(options.SigningKey)
                || string.IsNullOrWhiteSpace(options.Issuer)
                || string.IsNullOrWhiteSpace(options.Audience))
            {
                throw new InvalidOperationException(
                    "'Jwt' configuration requires non-empty Issuer, Audience, and SigningKey.");
            }

            return options;
        });
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Idempotent demo seeders (invoked on startup after migrations; tasks after the user).
        services.AddScoped<DemoUserSeeder>();
        services.AddScoped<DemoTaskSeeder>();

        return services;
    }
}
