using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ChocolateyPackageBuilder.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.Cli;

public sealed class BuildCommand : Command<BuildCommand.Settings>
{
    protected override int Execute([NotNull] CommandContext context, [NotNull] Settings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.InstallerPath))
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[cyan]Chocolatey Package Builder - Build[/]"));

                settings.InstallerPath = AnsiConsole.Prompt(
                    new TextPrompt<string>("Path to the installer file:")
                        .Validate(path =>
                            File.Exists(path)
                                ? ValidationResult.Success()
                                : ValidationResult.Error("[red]File not found.[/]")));

                settings.Type = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Installer type:")
                        .AddChoices("auto", "msi", "inno", "nsis", "scaffold"));

                var defaultName =
                    PackageUtility.CreatePackageSlug(Path.GetFileNameWithoutExtension(settings.InstallerPath));
                settings.Name = AnsiConsole.Prompt(
                    new TextPrompt<string>("Package name:")
                        .DefaultValue(defaultName));

                settings.Version = AnsiConsole.Prompt(
                    new TextPrompt<string>("Package version:")
                        .DefaultValue("1.0.0"));

                settings.Maintainer = AnsiConsole.Prompt(
                    new TextPrompt<string>("Maintainer:")
                        .DefaultValue(PackageUtility.DefaultMaintainer()));

                settings.Description = AnsiConsole.Prompt(
                    new TextPrompt<string>("Description:")
                        .DefaultValue($"Chocolatey package for {settings.Name}."));

                settings.Output = AnsiConsole.Prompt(
                    new TextPrompt<string>("Output directory:")
                        .DefaultValue(Environment.CurrentDirectory));
            }

            var installerPath = settings.InstallerPath;
            if (!File.Exists(installerPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Installer not found at '{Markup.Escape(installerPath)}'.");
                return 1;
            }

            var type = ResolveInstallerType(settings.Type, installerPath);
            var packageName = string.IsNullOrWhiteSpace(settings.Name)
                ? PackageUtility.CreatePackageSlug(Path.GetFileNameWithoutExtension(installerPath))
                : settings.Name.Trim();
            var version = string.IsNullOrWhiteSpace(settings.Version) ? "1.0.0" : settings.Version.Trim();
            var maintainer = string.IsNullOrWhiteSpace(settings.Maintainer)
                ? PackageUtility.DefaultMaintainer()
                : settings.Maintainer.Trim();
            var description = string.IsNullOrWhiteSpace(settings.Description)
                ? $"Chocolatey package for {packageName}."
                : settings.Description.Trim();
            var outputDir = string.IsNullOrWhiteSpace(settings.Output)
                ? Environment.CurrentDirectory
                : settings.Output.Trim();

            if (settings.Type.Equals("auto", StringComparison.OrdinalIgnoreCase) && type == InstallerType.Unknown)
                AnsiConsole.MarkupLine(
                    "[yellow]Warning:[/] Installer type was not detected. Scaffolded template silent arguments need review.");

            var result = PackageGenerator.Generate(new PackageBuildRequest(
                installerPath,
                type,
                packageName,
                version,
                maintainer,
                description,
                outputDir));

            if (result.IsScaffold)
            {
                AnsiConsole.MarkupLine($"[yellow]Scaffolded template:[/] {Markup.Escape(result.OutputPath)}");
                AnsiConsole.MarkupLine(
                    $"Review tools/chocolateyInstall.ps1, then run: ChocolateyPackageBuilder pack \"{Markup.Escape(result.OutputPath)}\"");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Built package:[/] {Markup.Escape(result.OutputPath)}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }


    private static InstallerType ResolveInstallerType(string value, string installerPath)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "auto" => InstallerDetector.Detect(installerPath),
            "msi" => InstallerType.Msi,
            "inno" => InstallerType.InnoSetup,
            "nsis" => InstallerType.Nsis,
            "scaffold" => InstallerType.Unknown,
            _ => throw new ArgumentException("--type must be one of: auto, msi, inno, nsis, scaffold.")
        };
    }


    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[installerPath]")]
        [Description("Path to the installer file.")]
        public string? InstallerPath { get; set; }

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

        [CommandOption("-t|--type")]
        [Description("Installer type: auto, msi, inno, nsis, or scaffold.")]
        public string Type { get; set; } = "auto";
    }
}