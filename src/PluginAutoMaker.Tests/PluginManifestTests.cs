
using PluginAutoMaker.Spec.Models;

using Xunit;

namespace PluginAutoMaker.Tests;

public class PluginManifestTests
{
    [Fact]
    public void ToYaml_IncludesApiVersionAndMain()
    {
        var spec = new PluginSpecification
        {
            PluginName = "Sample",
            PluginId = "sample",
            PackageName = "com.example.sample",
            MainClass = "com.example.sample.Main",
            Version = "1.0.0",
            ApiVersion = "1.20"
        };

        var yaml = spec.ToYaml();

        Assert.Contains("main: com.example.sample.Main", yaml);
        Assert.Contains("api-version: 1.20", yaml);
    }
}
