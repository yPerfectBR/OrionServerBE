using Orion.Config;
using Orion.World.Threading;

namespace Orion.World.Tests;

public sealed class AreaResolverTests
{
    [Fact]
    public void ResolveArea_ReturnsDefaultOutsideConfiguredAreas()
    {
        AreaResolver resolver = new([
            new ThreadingAreaConfig { Name = "city", Start = [-10, -10], End = [10, 10] }
        ]);

        Assert.Equal(AreaResolver.DefaultThread, resolver.ResolveArea(20, 20));
    }

    [Fact]
    public void ResolveArea_ReturnsDedicatedThreadInsideArea()
    {
        AreaResolver resolver = new([
            new ThreadingAreaConfig { Name = "city_a", Start = [0, 0], End = [10, 10] },
            new ThreadingAreaConfig { Name = "city_b", Start = [20, 20], End = [30, 30] }
        ]);

        Assert.Equal(1, resolver.ResolveArea(5, 5));
        Assert.Equal(2, resolver.ResolveArea(25, 25));
        Assert.Equal(AreaResolver.DefaultThread, resolver.ResolveArea(15, 15));
    }
}
