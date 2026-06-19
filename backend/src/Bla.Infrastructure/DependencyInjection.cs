using Bla.Infrastructure.Migrations;
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

        // Feature agents: register repositories, password hasher, and JWT token service here, e.g.
        //   services.AddScoped<ITaskRepository, NpgsqlTaskRepository>();
        //   services.AddScoped<IUserRepository, NpgsqlUserRepository>();
        //   services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        //   services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
