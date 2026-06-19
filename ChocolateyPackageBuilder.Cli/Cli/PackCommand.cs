using System.ComponentModel;
using ChocolateyPackageBuilder.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.Cli;

public sealed class PackCommand : Command<PackCommand.Settings>
{
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.DirectoryPath))
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[cyan]Chocolatey Package Builder - Pack[/]"));

                settings.DirectoryPath = AnsiConsole.Prompt(
                    new TextPrompt<string>("Path to the scaffolded template directory:")
                        .Validate(path =>
                            Directory.Exists(path)
                                ? ValidationResult.Success()
                                : ValidationResult.Error("[red]Directory not found.[/]")));
            }

            var result = PackageGenerator.PackScaffold(settings.DirectoryPath);
            AnsiConsole.MarkupLine($"[green]Packed scaffold:[/] {Markup.Escape(result.OutputPath)}");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[directoryPath]")]
        [Description("Path to the scaffolded template directory containing a .nuspec file.")]
        public string? DirectoryPath { get; set; }
    }
}