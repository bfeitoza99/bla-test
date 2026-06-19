namespace Bla.Infrastructure.Migrations;

/// <summary>
/// Configuration for the <see cref="MigrationRunner"/>.
/// </summary>
public sealed class MigrationOptions
{
    /// <summary>
    /// Absolute or relative path to the directory holding the <c>*.sql</c> migration scripts.
    /// Defaults to the <c>db/migrations</c> folder shipped next to the build output.
    /// </summary>
    public string MigrationsPath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "db", "migrations");
}
