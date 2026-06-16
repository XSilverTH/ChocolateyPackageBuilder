using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.Commands;

public class PackCommand : Command<PackCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<directoryPath>")]
        [Description("Path to the scaffolded template directory containing a .nuspec file.")]
        public string DirectoryPath { get; set; } = string.Empty;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var dirPath = settings.DirectoryPath;
        if (!System.IO.Directory.Exists(dirPath))
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found at '{dirPath}'.");
            return 1;
        }

        var nuspecFiles = System.IO.Directory.GetFiles(dirPath, "*.nuspec");
        if (nuspecFiles.Length == 0)
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error:[/] No .nuspec file found in '{dirPath}'.");
            return 1;
        }
        if (nuspecFiles.Length > 1)
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error:[/] Multiple .nuspec files found in '{dirPath}'. Please leave only one.");
            return 1;
        }

        var nuspecPath = nuspecFiles[0];
        
        using var fs = new System.IO.FileStream(nuspecPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        var builder = new NuGet.Packaging.PackageBuilder(fs, dirPath);
        
        var outputPath = System.IO.Path.Combine(System.IO.Directory.GetParent(System.IO.Path.GetFullPath(dirPath))?.FullName ?? ".", $"{builder.Id}.{builder.Version}.nupkg");
        
        using (var outFs = new System.IO.FileStream(outputPath, System.IO.FileMode.Create))
        {
            builder.Save(outFs);
        }

        Spectre.Console.AnsiConsole.MarkupLine($"[green]Successfully packed scaffold into:[/] {outputPath}");
        return 0;
    }
}