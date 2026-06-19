using System.ComponentModel;
using ChocolateyPackageBuilder.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.Cli.Commands;

public sealed class PackCommand : AsyncCommand<PackCommand.Settings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.Path))
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[cyan]Chocolatey Package Builder - Pack[/]"));

                settings.Path = AnsiConsole.Prompt(
                    new TextPrompt<string>("Path to scaffold directory or .cpbproj project file:")
                        .Validate(path =>
                            Directory.Exists(path) || File.Exists(path)
                                ? ValidationResult.Success()
                                : ValidationResult.Error("[red]Path not found.[/]")));
            }

            var packPath = settings.Path.Trim();
            if (File.Exists(packPath) &&
                System.IO.Path.GetExtension(packPath)
                    .Equals(CustomInstallerProjectStore.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                var outputDir = string.IsNullOrWhiteSpace(settings.Output)
                    ? Environment.CurrentDirectory
                    : settings.Output.Trim();
                var result = await PackageGenerator.PackCustomProjectAsync(
                    new CustomProjectPackRequest(packPath, outputDir), cancellationToken);
                AnsiConsole.MarkupLine($"[green]Packed project:[/] {Markup.Escape(result.OutputPath)}");
                return 0;
            }

            if (Directory.Exists(packPath))
            {
                var result = PackageGenerator.PackScaffold(packPath, settings.Output);
                AnsiConsole.MarkupLine($"[green]Packed scaffold:[/] {Markup.Escape(result.OutputPath)}");
                return 0;
            }

            AnsiConsole.MarkupLine("[red]Error:[/] Pack path must be a scaffold directory or .cpbproj project file.");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[path]")]
        [Description("Path to a scaffold directory or .cpbproj project file.")]
        public string? Path { get; set; }

        [CommandOption("-o|--output")]
        [Description("Output directory for the generated .nupkg. Defaults to the scaffold parent directory or current directory for .cpbproj files.")]
        public string? Output { get; set; }
    }
}