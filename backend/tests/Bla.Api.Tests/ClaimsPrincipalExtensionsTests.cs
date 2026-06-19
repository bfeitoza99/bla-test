using System.Security.Claims;
using Bla.Api.Authentication;
using FluentAssertions;

namespace Bla.Api.Tests;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(string claimType, string value) =>
        new(new ClaimsIdentity([new Claim(claimType, value)], authenticationType: "Test"));

    [Fact]
    public void GetUserId_ReturnsGuid_FromNameIdentifierClaim()
    {
        var id = Guid.NewGuid();
        var principal = PrincipalWith(ClaimTypes.NameIdentifier, id.ToString());

        principal.GetUserId().Should().Be(id);
    }

    [Fact]
    public void GetUserId_ReturnsGuid_FromSubClaim()
    {
        var id = Guid.NewGuid();
        var principal = PrincipalWith("sub", id.ToString());

        principal.GetUserId().Should().Be(id);
    }

    [Fact]
    public void GetUserId_Throws_WhenClaimMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var act = () => principal.GetUserId();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetUserId_Throws_WhenClaimNotAGuid()
    {
        var principal = PrincipalWith(ClaimTypes.NameIdentifier, "not-a-guid");

        var act = () => principal.GetUserId();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TryGetUserId_ReturnsTrueAndId_WhenPresent()
    {
        var id = Guid.NewGuid();
        var principal = PrincipalWith(ClaimTypes.NameIdentifier, id.ToString());

        var ok = principal.TryGetUserId(out var parsed);

        ok.Should().BeTrue();
        parsed.Should().Be(id);
    }

    [Fact]
    public void TryGetUserId_ReturnsFalse_WhenMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var ok = principal.TryGetUserId(out var parsed);

        ok.Should().BeFalse();
        parsed.Should().Be(Guid.Empty);
    }
}
