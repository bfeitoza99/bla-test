namespace Bla.Domain.Tasks;

/// <summary>
/// A task owned by a single user: a titled, optionally described unit of work with a lifecycle
/// <see cref="Status"/> and a due date. Invariants (non-empty title, owner, UTC timestamps) are
/// guarded at construction and on <see cref="Update"/>. The type carries no I/O and no framework
/// dependency.
/// </summary>
public sealed class TaskItem
{
    private TaskItem(
        Guid id,
        string title,
        string? description,
        TaskStatus status,
        DateTime dueDate,
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        UserId = userId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Primary key.</summary>
    public Guid Id { get; }

    /// <summary>Short, non-empty title.</summary>
    public string Title { get; private set; }

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; private set; }

    /// <summary>Lifecycle state.</summary>
    public TaskStatus Status { get; private set; }

    /// <summary>Due date, always UTC.</summary>
    public DateTime DueDate { get; private set; }

    /// <summary>Owning user's id (FK → users). Tasks are scoped to their owner.</summary>
    public Guid UserId { get; }

    /// <summary>Creation timestamp, always UTC.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Last-modified timestamp, always UTC. Bumped by <see cref="Update"/>.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new task, enforcing invariants and normalizing the title (trimmed).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// When the id or user id is empty, the title is missing, or a timestamp/due date is not UTC.
    /// </exception>
    public static TaskItem Create(
        Guid id,
        string title,
        string? description,
        TaskStatus status,
        DateTime dueDate,
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task id must not be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id must not be empty.", nameof(userId));
        }

        var normalizedTitle = NormalizeTitle(title);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Status must be a defined TaskStatus value.", nameof(status));
        }

        if (dueDate.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DueDate must be UTC.", nameof(dueDate));
        }

        if (createdAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("CreatedAt must be UTC.", nameof(createdAt));
        }

        if (updatedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("UpdatedAt must be UTC.", nameof(updatedAt));
        }

        return new TaskItem(
            id,
            normalizedTitle,
            NormalizeDescription(description),
            status,
            dueDate,
            userId,
            createdAt,
            updatedAt);
    }

    /// <summary>
    /// Rehydrates a task from a trusted store (e.g. the database), guarding the non-empty
    /// invariants but treating the stored values as already valid.
    /// </summary>
    public static TaskItem Restore(
        Guid id,
        string title,
        string? description,
        TaskStatus status,
        DateTime dueDate,
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task id must not be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id must not be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title must not be empty.", nameof(title));
        }

        return new TaskItem(id, title, description, status, dueDate, userId, createdAt, updatedAt);
    }

    /// <summary>
    /// Applies an edit to the mutable fields and bumps <see cref="UpdatedAt"/>. Re-guards the
    /// invariants so a task can never be edited into an invalid state.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// When the title is missing, the status is undefined, or a timestamp is not UTC.
    /// </exception>
    public void Update(
        string title,
        string? description,
        TaskStatus status,
        DateTime dueDate,
        DateTime updatedAt)
    {
        var normalizedTitle = NormalizeTitle(title);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Status must be a defined TaskStatus value.", nameof(status));
        }

        if (dueDate.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DueDate must be UTC.", nameof(dueDate));
        }

        if (updatedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("UpdatedAt must be UTC.", nameof(updatedAt));
        }

        Title = normalizedTitle;
        Description = NormalizeDescription(description);
        Status = status;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    /// <summary>Trims the title and rejects an empty result.</summary>
    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title must not be empty.", nameof(title));
        }

        return title.Trim();
    }

    /// <summary>Trims the description; an empty/whitespace description becomes <see langword="null"/>.</summary>
    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }
}
