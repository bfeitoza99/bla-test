using Bla.Domain.Users;
using Bla.Infrastructure.Security;
using FluentAssertions;

namespace Bla.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private static User SampleUser() =>
        User.Create(Guid.NewGuid(), "user@bla.local", "pending", DateTime.UtcNow);

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hasher = new PasswordHasher();
        var user = SampleUser();

        var hash = hasher.Hash(user, "Password1");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("Password1", "the hash must never be the plaintext");
        hasher.Verify(user, hash, "Password1").Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var user = SampleUser();
        var hash = hasher.Hash(user, "Password1");

        hasher.Verify(user, hash, "WrongPass1").Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword_DueToSalt()
    {
        var hasher = new PasswordHasher();
        var user = SampleUser();

        var first = hasher.Hash(user, "Password1");
        var second = hasher.Hash(user, "Password1");

        first.Should().NotBe(second);
    }
}
