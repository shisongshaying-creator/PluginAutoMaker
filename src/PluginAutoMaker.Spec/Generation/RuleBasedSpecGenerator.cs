using System.Text.RegularExpressions;
using PluginAutoMaker.Spec.Models;

namespace PluginAutoMaker.Spec.Generation;

public sealed class RuleBasedSpecGenerator : ISpecGenerator
{
    private static readonly Regex PluginNameRegex = new("プラグイン名\\s*[:：]\\s*(?<name>[A-Za-z0-9_\\- ]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex VersionRegex = new("バージョン\\s*[:：]\\s*(?<ver>[0-9A-Za-z_.-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<PluginSpecification> GenerateAsync(string requirementText, CancellationToken cancellationToken = default)
    {
        if (requirementText is null)
        {
            throw new ArgumentNullException(nameof(requirementText));
        }

        var normalized = requirementText.Replace("\r", string.Empty);
        var specification = new PluginSpecification();

        specification.PluginName = ExtractPluginName(normalized) ?? "AutoPlugin";
        specification.PluginId = SanitizePluginId(specification.PluginName);
        specification.PackageName = $"com.example.{specification.PluginId}";
        specification.MainClass = $"{specification.PackageName}.Main";
        specification.Version = ExtractVersion(normalized) ?? "1.0.0";
        specification.Description = BuildDescription(normalized, specification.PluginName);
        specification.ApiVersion = "1.20";

        foreach (var command in ExtractCommands(normalized))
        {
            specification.AddCommand(command);
        }

        if (!specification.Commands.Any())
        {
            specification.AddCommand(new PluginCommand
            {
                Name = "autoplugin",
                Description = "サンプルコマンド",
                Usage = "/autoplugin",
                Permission = "autoplugin.use"
            });
        }

        specification.ZipSources = normalized.Contains("zip", StringComparison.OrdinalIgnoreCase) || normalized.Contains("Zip化");

        return Task.FromResult(specification);
    }

    private static string? ExtractPluginName(string text)
    {
        var match = PluginNameRegex.Match(text);
        if (match.Success)
        {
            return match.Groups["name"].Value.Trim();
        }

        var inline = Regex.Match(text, @"(?<=プラグインは)[\s\S]{0,40}?『(?<name>[^』]+)』");
        if (inline.Success)
        {
            return inline.Groups["name"].Value.Trim();
        }

        return null;
    }

    private static string? ExtractVersion(string text)
    {
        var match = VersionRegex.Match(text);
        if (match.Success)
        {
            return match.Groups["ver"].Value.Trim();
        }

        return null;
    }

    private static IEnumerable<PluginCommand> ExtractCommands(string text)
    {
        var commands = new Dictionary<string, PluginCommand>(StringComparer.OrdinalIgnoreCase);
        var commandMatches = Regex.Matches(text, @"(/[a-zA-Z0-9_\-]+)([^\n]*)");
        foreach (Match match in commandMatches)
        {
            var raw = match.Groups[1].Value;
            var description = match.Groups[2].Value.Trim().Trim('。');
            var name = SanitizeCommand(raw[1..]);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            commands[name] = new PluginCommand
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(description) ? $"{raw} コマンド" : description,
                Usage = raw,
                Permission = $"{SanitizePluginId(name)}.use"
            };
        }

        return commands.Values;
    }

    private static string BuildDescription(string text, string pluginName)
    {
        var sentences = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var description = sentences.FirstOrDefault(s => s.Length > 5 && !s.Contains("/"))?.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            description = $"{pluginName} は自動生成されたPaperプラグインです。";
        }

        return description;
    }

    private static string SanitizePluginId(string name)
    {
        var id = new string(name.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "autoplugin";
        }

        return id;
    }

    private static string SanitizeCommand(string command)
    {
        var sanitized = new string(command.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return sanitized.ToLowerInvariant();
    }
}
