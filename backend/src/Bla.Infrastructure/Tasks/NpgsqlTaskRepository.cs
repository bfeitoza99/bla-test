using System.Data;
using Bla.Application.Common;
using Bla.Application.Tasks;
using Bla.Domain.Tasks;
using Npgsql;
using NpgsqlTypes;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Infrastructure.Tasks;

/// <summary>
/// <see cref="ITaskRepository"/> over raw ADO.NET (Npgsql). All SQL is parameterized; connections,
/// commands, and readers are disposed via <c>await using</c> so they return to the pool. Every query
/// is scoped by <c>user_id</c> so a caller can only ever touch their own tasks. The status enum is
/// stored as its text name; rows are mapped to the domain entity by hand.
/// </summary>
public sealed class NpgsqlTaskRepository : ITaskRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlTaskRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            INSERT INTO tasks
                (id, title, description, status, due_date, user_id, created_at, updated_at)
            VALUES
                (@id, @title, @description, @status, @due_date, @user_id, @created_at, @updated_at);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = task.Id });
        command.Parameters.Add(new NpgsqlParameter("title", NpgsqlDbType.Text) { Value = task.Title });
        command.Parameters.Add(new NpgsqlParameter("description", NpgsqlDbType.Text)
        {
            Value = (object?)task.Description ?? DBNull.Value,
        });
        command.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text) { Value = task.Status.ToString() });
        command.Parameters.Add(new NpgsqlParameter("due_date", NpgsqlDbType.TimestampTz) { Value = task.DueDate });
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = task.UserId });
        command.Parameters.Add(new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz) { Value = task.CreatedAt });
        command.Parameters.Add(new NpgsqlParameter("updated_at", NpgsqlDbType.TimestampTz) { Value = task.UpdatedAt });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT id, title, description, status, due_date, user_id, created_at, updated_at
            FROM tasks
            WHERE id = @id AND user_id = @user_id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userId });

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<PagedResult<TaskItem>> ListByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        TaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        // The optional status filter is appended to both the count and the page query identically so
        // the total always matches the filtered set. Values are parameterized, never interpolated.
        var statusFilter = status is null ? string.Empty : " AND status = @status";

        var countSql = $"SELECT COUNT(*) FROM tasks WHERE user_id = @user_id{statusFilter};";

        var pageSql =
            $"""
            SELECT id, title, description, status, due_date, user_id, created_at, updated_at
            FROM tasks
            WHERE user_id = @user_id{statusFilter}
            ORDER BY due_date, id
            LIMIT @limit OFFSET @offset;
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        long total;
        await using (var countCommand = new NpgsqlCommand(countSql, connection))
        {
            countCommand.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userId });
            if (status is not null)
            {
                countCommand.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text) { Value = status.ToString() });
            }

            total = (long)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
        }

        var items = new List<TaskItem>();
        await using (var pageCommand = new NpgsqlCommand(pageSql, connection))
        {
            pageCommand.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userId });
            if (status is not null)
            {
                pageCommand.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text) { Value = status.ToString() });
            }

            pageCommand.Parameters.Add(new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = pageSize });
            pageCommand.Parameters.Add(new NpgsqlParameter("offset", NpgsqlDbType.Integer) { Value = (page - 1) * pageSize });

            await using var reader = await pageCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(Map(reader));
            }
        }

        return new PagedResult<TaskItem>(items, page, pageSize, total);
    }

    public async Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            UPDATE tasks
            SET title = @title,
                description = @description,
                status = @status,
                due_date = @due_date,
                updated_at = @updated_at
            WHERE id = @id AND user_id = @user_id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("title", NpgsqlDbType.Text) { Value = task.Title });
        command.Parameters.Add(new NpgsqlParameter("description", NpgsqlDbType.Text)
        {
            Value = (object?)task.Description ?? DBNull.Value,
        });
        command.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text) { Value = task.Status.ToString() });
        command.Parameters.Add(new NpgsqlParameter("due_date", NpgsqlDbType.TimestampTz) { Value = task.DueDate });
        command.Parameters.Add(new NpgsqlParameter("updated_at", NpgsqlDbType.TimestampTz) { Value = task.UpdatedAt });
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = task.Id });
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = task.UserId });

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tasks WHERE id = @id AND user_id = @user_id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userId });

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    private static TaskItem Map(NpgsqlDataReader reader)
    {
        var id = reader.GetGuid(0);
        var title = reader.GetString(1);
        var description = reader.IsDBNull(2) ? null : reader.GetString(2);
        var status = Enum.Parse<TaskStatus>(reader.GetString(3));
        // Stored as timestamptz; Npgsql returns a UTC DateTime for these.
        var dueDate = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc);
        var userId = reader.GetGuid(5);
        var createdAt = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc);
        var updatedAt = DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc);

        return TaskItem.Restore(id, title, description, status, dueDate, userId, createdAt, updatedAt);
    }
}
