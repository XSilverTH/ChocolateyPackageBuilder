using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;
using Spectre.Console;

namespace ChocolateyPackageBuilder.Commands;

public class BuildCommand : Command<BuildCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<installerPath>")]
        [Description("Path to the installer file.")]
        public string InstallerPath { get; set; } = string.Empty;

        [CommandOption("-n|--name")]
        [Description("The name of the package.")]
        public string? Name { get; set; }

        [CommandOption("-v|--version")]
        [Description("The version of the package.")]
        public string? Version { get; set; }

        [CommandOption("-m|--maintainer")]
        [Description("The maintainer of the package.")]
        public string? Maintainer { get; set; }

        [CommandOption("-d|--description")]
        [Description("The description of the package.")]
        public string? Description { get; set; }

        [CommandOption("-o|--output")]
        [Description("The output directory for the generated .nupkg or scaffold.")]
        public string? Output { get; set; }
    }

    protected override int Execute([NotNull] CommandContext context, [NotNull] Settings settings, CancellationToken cancellationToken)
    {
        var installerPath = settings.InstallerPath;
        if (!System.IO.File.Exists(installerPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Installer not found at '{installerPath}'.");
            return 1;
        }

        var type = ChocolateyPackageBuilder.Services.InstallerDetector.Detect(installerPath);
        if (type == ChocolateyPackageBuilder.Services.InstallerType.Unknown)
        {
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Could not automatically detect the installer type. Please [green]select the type[/]:")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "InnoSetup",
                        "NSIS",
                        "MSI",
                        "Other (Scaffold Template)"
                    }));
            
            type = selection switch
            {
                "InnoSetup" => ChocolateyPackageBuilder.Services.InstallerType.InnoSetup,
                "NSIS" => ChocolateyPackageBuilder.Services.InstallerType.Nsis,
                "MSI" => ChocolateyPackageBuilder.Services.InstallerType.Msi,
                _ => ChocolateyPackageBuilder.Services.InstallerType.Unknown
            };
        }

        AnsiConsole.MarkupLine($"[green]Proceeding with Installer Type:[/] {type}");
        
        var name = settings.Name ?? System.IO.Path.GetFileNameWithoutExtension(installerPath);
        var version = settings.Version ?? "1.0.0";
        var maintainer = settings.Maintainer ?? "Unknown";
        var description = settings.Description ?? "Auto-generated Chocolatey package.";
        var outputDir = settings.Output ?? Environment.CurrentDirectory;
        var installerFileName = System.IO.Path.GetFileName(installerPath);

        var scriptContent = ChocolateyPackageBuilder.Services.ScriptGenerator.Generate(type, name, installerFileName);

        if (type == ChocolateyPackageBuilder.Services.InstallerType.Unknown)
        {
            ChocolateyPackageBuilder.Services.PackageGenerator.GenerateScaffold(
                installerPath, installerFileName, scriptContent, name, version, maintainer, description, outputDir);
        }
        else
        {
            ChocolateyPackageBuilder.Services.PackageGenerator.GenerateAuto(
                installerPath, installerFileName, scriptContent, name, version, maintainer, description, outputDir);
        }

        return 0;
    }
}