using System;
using System.ComponentModel;
using System.Threading;
using ChocolateyPackageBuilder.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.App.Cli;

public sealed class PackCommand : Command<PackCommand.Settings>
{
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
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
        [CommandArgument(0, "<directoryPath>")]
        [Description("Path to the scaffolded template directory containing a .nuspec file.")]
        public string DirectoryPath { get; set; } = string.Empty;
    }
}