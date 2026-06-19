using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bla.Infrastructure.Migrations;

/// <summary>
/// Lightweight, idempotent SQL migration runner.
/// Applies pending <c>*.sql</c> scripts from <see cref="MigrationOptions.MigrationsPath"/> in
/// filename order, recording each applied script in a <c>schema_migrations</c> table so reruns
/// are no-ops. Each script runs inside its own transaction together with the bookkeeping insert,
/// so a failed script leaves the database unchanged.
/// </summary>
/// <remarks>
/// Scripts are ordered by file name; the convention is a sortable numeric prefix
/// (e.g. <c>0001_create_users.sql</c>, <c>0002_create_tasks.sql</c>). Feature agents own the
/// feature tables — this runner only owns the mechanism and the <c>schema_migrations</c> ledger.
/// </remarks>
public sealed class MigrationRunner
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly MigrationOptions _options;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(
        NpgsqlDataSource dataSource,
        MigrationOptions options,
        ILogger<MigrationRunner> logger)
    {
        _dataSource = dataSource;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Applies all pending migrations. Safe to call on every startup.
    /// </summary>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMigrationsTableAsync(cancellationToken);

        var applied = await GetAppliedMigrationsAsync(cancellationToken);
        var scripts = DiscoverScripts();

        var pending = scripts.Where(s => !applied.Contains(s.Id)).ToList();
        if (pending.Count == 0)
        {
            _logger.LogInformation("Database schema is up to date; no pending migrations.");
            return;
        }

        foreach (var script in pending)
        {
            await ApplyAsync(script, cancellationToken);
            _logger.LogInformation("Applied migration {MigrationId}.", script.Id);
        }
    }

    private async Task EnsureMigrationsTableAsync(CancellationToken cancellationToken)
    {
        const string sql =
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                id          TEXT        NOT NULL PRIMARY KEY,
                checksum    TEXT        NOT NULL,
                applied_at  TIMESTAMPTZ NOT NULL DEFAULT now()
            );
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<HashSet<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id FROM schema_migrations;", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private IReadOnlyList<MigrationScript> DiscoverScripts()
    {
        if (!Directory.Exists(_options.MigrationsPath))
        {
            _logger.LogWarning(
                "Migrations directory {Path} does not exist; nothing to apply.",
                _options.MigrationsPath);
            return [];
        }

        return Directory
            .EnumerateFiles(_options.MigrationsPath, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(path =>
            {
                var sql = File.ReadAllText(path);
                return new MigrationScript(
                    Id: Path.GetFileNameWithoutExtension(path),
                    Sql: sql,
                    Checksum: Checksum(sql));
            })
            .ToList();
    }

    private async Task ApplyAsync(MigrationScript script, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var scriptCommand = new NpgsqlCommand(script.Sql, connection, transaction))
        {
            await scriptCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var ledgerCommand = new NpgsqlCommand(
            "INSERT INTO schema_migrations (id, checksum) VALUES (@id, @checksum);",
            connection,
            transaction))
        {
            ledgerCommand.Parameters.AddWithValue("id", script.Id);
            ledgerCommand.Parameters.AddWithValue("checksum", script.Checksum);
            await ledgerCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static string Checksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    private sealed record MigrationScript(string Id, string Sql, string Checksum);
}
