using Bla.Infrastructure;
using Bla.Infrastructure.Migrations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bla.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersMigrationOptions_FromConfiguredPath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Migrations:Path"] = "/custom/migrations",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MigrationOptions>();

        options.MigrationsPath.Should().Be("/custom/migrations");
    }

    [Fact]
    public void AddInfrastructure_UsesDefaultMigrationsPath_WhenNotConfigured()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MigrationOptions>();

        options.MigrationsPath.Should().EndWith(Path.Combine("db", "migrations"));
    }
}
