using System.Text;

namespace PluginAutoMaker.Spec.Models;

public sealed class PluginSpecification
{
    public string PluginName { get; set; } = "AutoPlugin";

    public string PluginId { get; set; } = "autoplugin";

    public string PackageName { get; set; } = "com.example.autoplugin";

    public string MainClass { get; set; } = "com.example.autoplugin.Main";

    public string Version { get; set; } = "1.0.0";

    public string Description { get; set; } = "自動生成されたPaperプラグイン";

    public string ApiVersion { get; set; } = "1.20";

    public IReadOnlyList<PluginCommand> Commands => _commands;

    public bool ZipSources { get; set; }

    public string OutputJarFileName => $"{PluginId}.jar";

    public string SourceArchiveName => $"{PluginId}-src.zip";

    private readonly List<PluginCommand> _commands = new();

    public void AddCommand(PluginCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        _commands.Add(command);
    }

    public string ToYaml()
    {
        return PluginSpecificationYamlSerializer.ToYaml(this);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PluginName: {PluginName}");
        sb.AppendLine($"PluginId: {PluginId}");
        sb.AppendLine($"PackageName: {PackageName}");
        sb.AppendLine($"MainClass: {MainClass}");
        sb.AppendLine($"Version: {Version}");
        sb.AppendLine($"Commands: {Commands.Count}");
        return sb.ToString();
    }
}
