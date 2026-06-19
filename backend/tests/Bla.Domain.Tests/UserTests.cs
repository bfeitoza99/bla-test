using Bla.Domain.Users;
using FluentAssertions;

namespace Bla.Domain.Tests;

/// <summary>
/// Invariants for the <see cref="User"/> entity: identity, normalized email, a non-empty
/// password hash, and a UTC creation timestamp. No I/O is involved.
/// </summary>
public class UserTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var user = User.Create(id, "person@example.com", "hash-value", createdAt);

        user.Id.Should().Be(id);
        user.Email.Should().Be("person@example.com");
        user.PasswordHash.Should().Be("hash-value");
        user.CreatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("Person@Example.COM", "person@example.com")]
    [InlineData("  USER@BLA.LOCAL  ", "user@bla.local")]
    public void Create_NormalizesEmailToTrimmedLowerCase(string input, string expected)
    {
        var user = User.Create(Guid.NewGuid(), input, "hash", DateTime.UtcNow);

        user.Email.Should().Be(expected);
    }

    [Fact]
    public void Create_WithEmptyId_Throws()
    {
        var act = () => User.Create(Guid.Empty, "person@example.com", "hash", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingEmail_Throws(string? email)
    {
        var act = () => User.Create(Guid.NewGuid(), email!, "hash", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingPasswordHash_Throws(string? hash)
    {
        var act = () => User.Create(Guid.NewGuid(), "person@example.com", hash!, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNonUtcCreatedAt_Throws()
    {
        var local = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var act = () => User.Create(Guid.NewGuid(), "person@example.com", "hash", local);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Restore_RehydratesWithoutNormalizing()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.SpecifyKind(new DateTime(2026, 1, 2, 3, 4, 5), DateTimeKind.Utc);

        var user = User.Restore(id, "stored@bla.local", "stored-hash", createdAt);

        user.Id.Should().Be(id);
        user.Email.Should().Be("stored@bla.local");
        user.PasswordHash.Should().Be("stored-hash");
        user.CreatedAt.Should().Be(createdAt);
    }
}
