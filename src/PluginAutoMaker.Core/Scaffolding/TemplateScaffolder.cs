using System.IO;
using System.Text;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Orchestration;
using PluginAutoMaker.Spec.Models;

namespace PluginAutoMaker.Core.Scaffolding;

public sealed class TemplateScaffolder : IScaffolder
{
    private readonly ILogSink _log;

    public TemplateScaffolder(ILogSink log)
    {
        _log = log;
    }

    public Task<ScaffoldResult> ScaffoldAsync(AutomationContext context, CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var spec = context.Specification;
        var projectDir = context.ProjectDirectory;
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "src", "main", "java"));
        Directory.CreateDirectory(Path.Combine(projectDir, "src", "main", "resources"));
        Directory.CreateDirectory(Path.Combine(projectDir, "gradle", "wrapper"));

        CreateSettingsGradle(projectDir, spec);
        CreateBuildGradle(projectDir, spec);

        var packagePath = Path.Combine(projectDir, "src", "main", "java", spec.PackageName.Replace('.', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(packagePath);
        var mainClassPath = Path.Combine(packagePath, "Main.java");
        File.WriteAllText(mainClassPath, BuildMainClass(spec), Encoding.UTF8);

        var resourcesPath = Path.Combine(projectDir, "src", "main", "resources");
        var pluginYamlPath = Path.Combine(resourcesPath, "plugin.yml");
        File.WriteAllText(pluginYamlPath, spec.ToYaml(), Encoding.UTF8);

        CreateGradleWrapperScripts(projectDir, context.GradleVersion);

        _log.Publish(LogLevel.Information, "内蔵テンプレートでGradleプロジェクトを生成しました。");

        return Task.FromResult(new ScaffoldResult
        {
            ProjectDirectory = projectDir,
            GradleWrapperPath = Path.Combine(projectDir, OperatingSystem.IsWindows() ? "gradlew.bat" : "gradlew"),
            BuildGradlePath = Path.Combine(projectDir, "build.gradle"),
            PluginYamlPath = pluginYamlPath,
            SourceMainClassPath = mainClassPath
        });
    }

    private static void CreateSettingsGradle(string projectDir, PluginSpecification spec)
    {
        var content = $"rootProject.name = \"{spec.PluginId}\"{Environment.NewLine}";
        File.WriteAllText(Path.Combine(projectDir, "settings.gradle"), content, Encoding.UTF8);
    }

    private static void CreateBuildGradle(string projectDir, PluginSpecification spec)
    {
        var builder = new StringBuilder();
        builder.AppendLine("plugins {");
        builder.AppendLine("    id 'java'");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("java {");
        builder.AppendLine("    toolchain {");
        builder.AppendLine("        languageVersion = JavaLanguageVersion.of(17)");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("group = '" + spec.PackageName + "'");
        builder.AppendLine("version = '" + spec.Version + "'");
        builder.AppendLine();
        builder.AppendLine("repositories {");
        builder.AppendLine("    mavenCentral()");
        builder.AppendLine("    maven { url = uri('https://repo.papermc.io/repository/maven-public/') }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("dependencies {");
        builder.AppendLine("    compileOnly 'io.papermc.paper:paper-api:1.20.1-R0.1-SNAPSHOT'");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("tasks.withType(JavaCompile).configureEach {");
        builder.AppendLine("    options.encoding = 'UTF-8'");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("tasks.jar { archiveFileName = '" + spec.OutputJarFileName + "' }");
        builder.AppendLine();
        builder.AppendLine("tasks.register('copyPluginJar', Copy) {");
        builder.AppendLine("    from tasks.jar");
        builder.AppendLine("    into layout.buildDirectory.dir('dist')");
        builder.AppendLine("}");

        File.WriteAllText(Path.Combine(projectDir, "build.gradle"), builder.ToString(), Encoding.UTF8);
    }

    private static string BuildMainClass(PluginSpecification spec)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"package {spec.PackageName};");
        builder.AppendLine();
        builder.AppendLine("import net.kyori.adventure.text.Component;");
        builder.AppendLine("import org.bukkit.command.Command;");
        builder.AppendLine("import org.bukkit.command.CommandSender;");
        builder.AppendLine("import org.bukkit.plugin.java.JavaPlugin;");
        builder.AppendLine();
        builder.AppendLine("public final class Main extends JavaPlugin {");
        builder.AppendLine("    @Override");
        builder.AppendLine("    public void onEnable() {");
        builder.AppendLine($"        getLogger().info(\"{EscapeJavaString(spec.PluginName)} v{EscapeJavaString(spec.Version)} を有効化しました。\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    @Override");
        builder.AppendLine("    public void onDisable() {");
        builder.AppendLine($"        getLogger().info(\"{EscapeJavaString(spec.PluginName)} v{EscapeJavaString(spec.Version)} を無効化しました。\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    @Override");
        builder.AppendLine("    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {");

        foreach (var command in spec.Commands)
        {
            builder.AppendLine($"        if (command.getName().equalsIgnoreCase(\"{EscapeJavaString(command.Name)}\")) {{");
            builder.AppendLine($"            sender.sendMessage(Component.text(\"{EscapeJavaString(command.Description)}\"));");
            builder.AppendLine("            return true;");
            builder.AppendLine("        }");
        }

        builder.AppendLine("        sender.sendMessage(Component.text(\"未知のコマンドです。\"));");
        builder.AppendLine("        return true;");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static void CreateGradleWrapperScripts(string projectDir, string gradleVersion)
    {
        var version = string.IsNullOrWhiteSpace(gradleVersion) ? "8.7" : gradleVersion;

        var gradlew = """#!/bin/sh
APP_HOME=$(cd "$(dirname "$0")" && pwd)
CLASSPATH="$APP_HOME/gradle/wrapper/gradle-wrapper.jar"
JAVA_EXE="java"
if [ -n "$JAVA_HOME" ]; then
  JAVA_EXE="$JAVA_HOME/bin/java"
fi
exec "$JAVA_EXE" -Dfile.encoding=UTF-8 -classpath "$CLASSPATH" org.gradle.wrapper.GradleWrapperMain "$@"
""";

        var gradlewBat = """@ECHO OFF
SET APP_HOME=%~dp0
SET CLASSPATH=%APP_HOME%gradle\wrapper\gradle-wrapper.jar
IF NOT "%JAVA_HOME%"=="" (
  SET JAVA_EXE=%JAVA_HOME%\bin\java.exe
) ELSE (
  SET JAVA_EXE=java
)
"%JAVA_EXE%" -Dfile.encoding=UTF-8 -classpath "%CLASSPATH%" org.gradle.wrapper.GradleWrapperMain %*
""";

        File.WriteAllText(Path.Combine(projectDir, "gradlew"), gradlew, Encoding.UTF8);
        File.WriteAllText(Path.Combine(projectDir, "gradlew.bat"), gradlewBat, Encoding.UTF8);

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                var gradlewPath = Path.Combine(projectDir, "gradlew");
                if (File.Exists(gradlewPath))
                {
                    File.SetUnixFileMode(gradlewPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }
            }
            catch
            {
                // ignore permission errors
            }
        }

        var properties = $"""distributionBase=GRADLE_USER_HOME
distributionPath=wrapper/dists
distributionUrl=https://services.gradle.org/distributions/gradle-{version}-bin.zip
zipStoreBase=GRADLE_USER_HOME
zipStorePath=wrapper/dists
""";

        File.WriteAllText(Path.Combine(projectDir, "gradle", "wrapper", "gradle-wrapper.properties"), properties, Encoding.UTF8);
    }

    private static string EscapeJavaString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }
}
