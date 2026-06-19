namespace Bla.Application.Users;

/// <summary>
/// Raised by the register use case when the email is already taken. The API maps this to
/// <c>409 Conflict</c>. Carries no other user's details — just signals the collision.
/// </summary>
public sealed class EmailAlreadyInUseException : Exception
{
    public EmailAlreadyInUseException()
        : base("An account with this email already exists.")
    {
    }
}
