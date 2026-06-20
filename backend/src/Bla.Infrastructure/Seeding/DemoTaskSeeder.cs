using Bla.Application.Tasks;
using Bla.Application.Users;
using Bla.Domain.Tasks;
using Bla.Domain.Users;
using Microsoft.Extensions.Logging;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Infrastructure.Seeding;

/// <summary>
/// Idempotent startup seed for sample tasks. Runs <em>after</em> <see cref="DemoUserSeeder"/>: looks
/// up the demo account (<c>demo@bla.local</c>) by email and, if that user has no tasks yet, inserts
/// a small, illustrative set spanning the three statuses. A no-op when the user is missing (the user
/// seed didn't run) or already has tasks.
/// </summary>
public sealed class DemoTaskSeeder
{
    private readonly IUserRepository _users;
    private readonly ITaskRepository _tasks;
    private readonly ILogger<DemoTaskSeeder> _logger;

    public DemoTaskSeeder(
        IUserRepository users,
        ITaskRepository tasks,
        ILogger<DemoTaskSeeder> logger)
    {
        _users = users;
        _tasks = tasks;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(DemoUserSeeder.DemoEmail);
        var demoUser = await _users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (demoUser is null)
        {
            _logger.LogInformation("Demo user not present; skipping demo task seed.");
            return;
        }

        // Idempotency: only seed when the demo user has no tasks at all.
        var existing = await _tasks.ListByUserAsync(demoUser.Id, page: 1, pageSize: 1, cancellationToken: cancellationToken);
        if (existing.Total > 0)
        {
            _logger.LogInformation("Demo user already has tasks; skipping demo task seed.");
            return;
        }

        var now = DateTime.UtcNow;
        var samples = new[]
        {
            BuildTask(demoUser.Id, "Prepare interview demo", "Walk through the Clean Architecture slices.", TaskStatus.InProgress, now.AddDays(2), now),
            BuildTask(demoUser.Id, "Write task CRUD tests", "Cover create, list, get, update, delete.", TaskStatus.Done, now.AddDays(-1), now),
            BuildTask(demoUser.Id, "Review API security", "Ownership scoping, validation, edge cases.", TaskStatus.Todo, now.AddDays(5), now),
        };

        foreach (var task in samples)
        {
            await _tasks.AddAsync(task, cancellationToken);
        }

        _logger.LogInformation("Seeded {Count} demo tasks for {Email}.", samples.Length, normalizedEmail);
    }

    private static TaskItem BuildTask(
        Guid userId,
        string title,
        string description,
        TaskStatus status,
        DateTime dueDate,
        DateTime now) =>
        TaskItem.Create(
            Guid.NewGuid(),
            title,
            description,
            status,
            DateTime.SpecifyKind(dueDate, DateTimeKind.Utc),
            userId,
            now,
            now);
}
