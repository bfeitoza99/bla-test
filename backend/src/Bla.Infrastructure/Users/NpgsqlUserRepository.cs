using System.Data;
using Bla.Application.Users;
using Bla.Domain.Users;
using Npgsql;
using NpgsqlTypes;

namespace Bla.Infrastructure.Users;

/// <summary>
/// <see cref="IUserRepository"/> over raw ADO.NET (Npgsql). All SQL is parameterized; connections,
/// commands, and readers are disposed via <c>await using</c> so they return to the pool. Maps
/// <see cref="System.Data.Common.DbDataReader"/> rows to the domain entity by hand.
/// </summary>
public sealed class NpgsqlUserRepository : IUserRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlUserRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<User?> GetByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT id, email, password_hash, created_at
            FROM users
            WHERE email = @email;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Text) { Value = normalizedEmail });

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT id, email, password_hash, created_at
            FROM users
            WHERE id = @id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            INSERT INTO users (id, email, password_hash, created_at)
            VALUES (@id, @email, @password_hash, @created_at);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = user.Id });
        command.Parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Text) { Value = user.Email });
        command.Parameters.Add(
            new NpgsqlParameter("password_hash", NpgsqlDbType.Text) { Value = user.PasswordHash });
        command.Parameters.Add(
            new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz) { Value = user.CreatedAt });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE email = @email);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Text) { Value = normalizedEmail });

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private static User Map(NpgsqlDataReader reader)
    {
        var id = reader.GetGuid(0);
        var email = reader.GetString(1);
        var passwordHash = reader.GetString(2);
        // Stored as timestamptz; Npgsql returns a UTC DateTime for it.
        var createdAt = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc);

        return User.Restore(id, email, passwordHash, createdAt);
    }
}
