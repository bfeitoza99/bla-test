using FluentAssertions;

namespace Bla.Domain.Tests;

/// <summary>
/// Placeholder so the Domain test project builds and runs green from the foundation.
/// Feature agents replace/extend with real domain entity tests (TDD).
/// </summary>
public class SmokeTests
{
    [Fact]
    public void DomainTestProject_IsWired()
    {
        true.Should().BeTrue();
    }
}
