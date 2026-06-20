namespace Bla.Domain.Tasks;

/// <summary>
/// The lifecycle state of a <see cref="TaskItem"/>. Persisted as its text name (snake/Pascal kept
/// stable) so the value is human-readable in the store and stable across renames of the numeric
/// backing.
/// </summary>
public enum TaskStatus
{
    /// <summary>Not started yet.</summary>
    Todo,

    /// <summary>Being worked on.</summary>
    InProgress,

    /// <summary>Completed.</summary>
    Done,
}
