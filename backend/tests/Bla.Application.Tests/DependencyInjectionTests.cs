using Bla.Application;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bla.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_BuildsServiceProvider_WithoutThrowing()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        var act = () => services.BuildServiceProvider(validateScopes: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddApplication_ReturnsSameCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }
}
