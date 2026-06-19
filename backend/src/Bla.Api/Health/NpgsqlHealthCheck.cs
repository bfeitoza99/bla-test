using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Bla.Api.Health;

/// <summary>
/// Readiness check that opens a connection and runs <c>SELECT 1</c> against PostgreSQL.
/// </summary>
/// <remarks>
/// Reports <see cref="HealthStatus.Unhealthy"/> (the configured failure status) when the database
/// is unreachable — it never throws out of the check, so the app boots and stays up even when
/// Postgres is unavailable at startup. The endpoint degrades; it does not crash the process.
/// </remarks>
public sealed class NpgsqlHealthCheck : IHealthCheck
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlHealthCheck(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1;", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database connection is available.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown / request abort — not a health failure to report on.
            throw;
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                description: "Database connection is unavailable.",
                exception: ex);
        }
    }
}
