namespace Bla.Application.Tasks;

/// <summary>
/// Raised when a task does not exist <em>or</em> is not owned by the requesting user. The API maps
/// this to <c>404 Not Found</c>. The two cases are deliberately indistinguishable so we never leak
/// the existence of another user's task.
/// </summary>
public sealed class TaskNotFoundException : Exception
{
    public TaskNotFoundException()
        : base("The task was not found.")
    {
    }
}
