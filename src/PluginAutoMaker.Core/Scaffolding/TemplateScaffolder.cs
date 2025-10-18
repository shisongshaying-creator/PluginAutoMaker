using System.Text;
using PluginAutoMaker.Core.Logging;
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

        CreateGradleWrapperScripts(projectDir);

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
        var commandHandlers = new StringBuilder();
        foreach (var command in spec.Commands)
        {
            commandHandlers.AppendLine("        if (command.getName().equalsIgnoreCase(\"" + command.Name + "\")) {");
            commandHandlers.AppendLine("            sender.sendMessage(Component.text(\"" + command.Description.Replace("\"", "\\\"") + "\"));");
            commandHandlers.AppendLine("            return true;");
            commandHandlers.AppendLine("        }");
        }

        return $"""
package {spec.PackageName};

import net.kyori.adventure.text.Component;
import org.bukkit.command.Command;
import org.bukkit.command.CommandSender;
import org.bukkit.plugin.java.JavaPlugin;

public final class Main extends JavaPlugin {{
    @Override
    public void onEnable() {{
        getLogger().info("{spec.PluginName} v{spec.Version} を有効化しました。");
    }}

    @Override
    public void onDisable() {{
        getLogger().info("{spec.PluginName} v{spec.Version} を無効化しました。");
    }}

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {{
{commandHandlers.ToString().TrimEnd()}
        sender.sendMessage(Component.text("未知のコマンドです。"));
        return true;
    }}
}}
""";
    }

    private static void CreateGradleWrapperScripts(string projectDir)
    {
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
SET CLASSPATH=%APP_HOME%gradle\\wrapper\\gradle-wrapper.jar
IF NOT "%JAVA_HOME%"=="" (
  SET JAVA_EXE=%JAVA_HOME%\\bin\\java.exe
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
                var fileInfo = new FileInfo(Path.Combine(projectDir, "gradlew"));
                fileInfo.Refresh();
                var current = fileInfo.Attributes;
                fileInfo.Attributes = current & ~FileAttributes.ReadOnly;
            }
            catch
            {
                // ignore
            }
        }

        var properties = """distributionBase=GRADLE_USER_HOME
distributionPath=wrapper/dists
distributionUrl=https\://services.gradle.org/distributions/gradle-8.7-bin.zip
zipStoreBase=GRADLE_USER_HOME
zipStorePath=wrapper/dists
""";
        File.WriteAllText(Path.Combine(projectDir, "gradle", "wrapper", "gradle-wrapper.properties"), properties, Encoding.UTF8);
    }
}
