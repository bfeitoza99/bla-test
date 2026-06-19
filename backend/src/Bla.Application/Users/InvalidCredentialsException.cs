namespace Bla.Application.Users;

/// <summary>
/// Raised by the login use case when authentication fails. Deliberately generic: it does not
/// reveal whether the email is unknown or the password is wrong. The API maps it to
/// <c>401 Unauthorized</c>.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
