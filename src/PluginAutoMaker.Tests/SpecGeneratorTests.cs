
using PluginAutoMaker.Spec.Generation;
using PluginAutoMaker.Spec.Models;

using Xunit;

namespace PluginAutoMaker.Tests;

public class SpecGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_PopulatesPluginName()
    {
        var generator = new RuleBasedSpecGenerator();
        var requirements = "プラグイン名: HelloPlugin
/hello コマンドで挨拶";

        var spec = await generator.GenerateAsync(requirements);

        Assert.Equal("HelloPlugin", spec.PluginName);
        Assert.Contains(spec.Commands, c => c.Name == "hello");
    }
}
