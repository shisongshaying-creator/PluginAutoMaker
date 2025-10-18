using System.Text;

namespace PluginAutoMaker.Spec.Models;

public static class PluginSpecificationYamlSerializer
{
    public static string ToYaml(PluginSpecification specification)
    {
        if (specification is null)
        {
            throw new ArgumentNullException(nameof(specification));
        }

        var builder = new StringBuilder();
        builder.AppendLine($"name: {specification.PluginName}");
        builder.AppendLine($"main: {specification.MainClass}");
        builder.AppendLine($"version: {specification.Version}");
        builder.AppendLine($"api-version: {specification.ApiVersion}");
        if (!string.IsNullOrWhiteSpace(specification.Description))
        {
            builder.AppendLine("description: |-");
            foreach (var line in specification.Description.Split('\n'))
            {
                builder.AppendLine($"  {line.TrimEnd()}".TrimEnd());
            }
        }

        if (specification.Commands.Any())
        {
            builder.AppendLine("commands:");
            foreach (var command in specification.Commands)
            {
                builder.AppendLine($"  {command.Name}:");
                if (!string.IsNullOrWhiteSpace(command.Description))
                {
                    builder.AppendLine($"    description: \"{Escape(command.Description)}\"");
                }

                if (!string.IsNullOrWhiteSpace(command.Usage))
                {
                    builder.AppendLine($"    usage: \"{Escape(command.Usage)}\"");
                }

                if (!string.IsNullOrWhiteSpace(command.Permission))
                {
                    builder.AppendLine($"    permission: {command.Permission}");
                }
            }
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}
