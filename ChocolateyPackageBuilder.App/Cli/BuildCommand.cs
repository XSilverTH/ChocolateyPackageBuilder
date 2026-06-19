using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading;
using ChocolateyPackageBuilder.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.App.Cli;

public sealed partial class BuildCommand : Command<BuildCommand.Settings>
{
    public sealed class Settings : CommandSettings
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

        [CommandOption("-t|--type")]
        [Description("Installer type: auto, msi, inno, nsis, or scaffold.")]
        public string Type { get; set; } = "auto";
    }

    protected override int Execute([NotNull] CommandContext context, [NotNull] Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var installerPath = settings.InstallerPath;
            if (!File.Exists(installerPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Installer not found at '{Markup.Escape(installerPath)}'.");
                return 1;
            }

            var type = ResolveInstallerType(settings.Type, installerPath);
            var packageName = string.IsNullOrWhiteSpace(settings.Name)
                ? CreatePackageSlug(Path.GetFileNameWithoutExtension(installerPath))
                : settings.Name.Trim();
            var version = string.IsNullOrWhiteSpace(settings.Version) ? "1.0.0" : settings.Version.Trim();
            var maintainer = string.IsNullOrWhiteSpace(settings.Maintainer) ? DefaultMaintainer() : settings.Maintainer.Trim();
            var description = string.IsNullOrWhiteSpace(settings.Description)
                ? $"Chocolatey package for {packageName}."
                : settings.Description.Trim();
            var outputDir = string.IsNullOrWhiteSpace(settings.Output) ? Environment.CurrentDirectory : settings.Output.Trim();

            if (settings.Type.Equals("auto", StringComparison.OrdinalIgnoreCase) && type == InstallerType.Unknown)
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] Installer type was not detected. Scaffolded template silent arguments need review.");
            }

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
                AnsiConsole.MarkupLine($"Review tools/chocolateyInstall.ps1, then run: ChocolateyPackageBuilder pack \"{Markup.Escape(result.OutputPath)}\"");
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

    internal static string CreatePackageSlug(string value)
    {
        var slug = SlugUnsafeCharacters().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "package" : slug;
    }

    internal static string DefaultMaintainer()
        => string.IsNullOrWhiteSpace(Environment.UserName) ? "Unknown" : Environment.UserName;

    private static InstallerType ResolveInstallerType(string value, string installerPath)
        => value.Trim().ToLowerInvariant() switch
        {
            "auto" => InstallerDetector.Detect(installerPath),
            "msi" => InstallerType.Msi,
            "inno" => InstallerType.InnoSetup,
            "nsis" => InstallerType.Nsis,
            "scaffold" => InstallerType.Unknown,
            _ => throw new ArgumentException("--type must be one of: auto, msi, inno, nsis, scaffold.")
        };

    [GeneratedRegex("[^a-z0-9.-]+")]
    private static partial Regex SlugUnsafeCharacters();
}
