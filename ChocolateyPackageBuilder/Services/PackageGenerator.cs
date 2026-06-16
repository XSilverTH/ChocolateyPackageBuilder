using NuGet.Packaging;
using NuGet.Versioning;
using Spectre.Console;

namespace ChocolateyPackageBuilder.Services;

public static class PackageGenerator
{
    public static void GenerateAuto(
        string installerPath, 
        string installerFileName,
        string scriptContent, 
        string packageName, 
        string version, 
        string maintainer, 
        string description,
        string outputDir)
    {
        var builder = new PackageBuilder
        {
            Id = packageName,
            Version = new NuGetVersion(version)
        };
        builder.Authors.Add(maintainer);
        builder.Description = description;

        // Add script from memory
        var scriptStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(scriptContent));
        builder.Files.Add(new StreamPackageFile(scriptStream, $@"tools\chocolateyInstall.ps1"));

        // Add installer from disk
        builder.Files.Add(new PhysicalPackageFile 
        { 
            SourcePath = installerPath, 
            TargetPath = $@"tools\{installerFileName}" 
        });

        // Ensure output dir exists
        if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var outputPath = Path.Combine(outputDir, $"{packageName}.{version}.nupkg");
        using (var fs = new FileStream(outputPath, FileMode.Create))
        {
            builder.Save(fs);
        }

        AnsiConsole.MarkupLine($"[green]Successfully built package:[/] {outputPath}");
    }

    public static void GenerateScaffold(
        string installerPath, 
        string installerFileName,
        string scriptContent, 
        string packageName, 
        string version, 
        string maintainer, 
        string description,
        string outputDir)
    {
        var templateDir = Path.Combine(outputDir, $"{packageName}-template");
        var toolsDir = Path.Combine(templateDir, "tools");

        Directory.CreateDirectory(toolsDir);

        // Copy installer
        var destInstallerPath = Path.Combine(toolsDir, installerFileName);
        File.Copy(installerPath, destInstallerPath, overwrite: true);

        // Write PS1
        var ps1Path = Path.Combine(toolsDir, "chocolateyInstall.ps1");
        File.WriteAllText(ps1Path, scriptContent);

        // Write nuspec
        var nuspecContent = $"""
                             <?xml version="1.0"?>
                             <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
                               <metadata>
                                 <id>{packageName}</id>
                                 <version>{version}</version>
                                 <authors>{maintainer}</authors>
                                 <description>{description}</description>
                               </metadata>
                             </package>
                             """;
        var nuspecPath = Path.Combine(templateDir, $"{packageName}.nuspec");
        File.WriteAllText(nuspecPath, nuspecContent);

        AnsiConsole.MarkupLine($"[yellow]Scaffolded template at:[/] {templateDir}");
        AnsiConsole.MarkupLine($"Edit the PS1 file, then run `[cyan]dotnet run -- pack \"{templateDir}\"[/]`");
    }
}

public class StreamPackageFile(Stream stream, string targetPath) : IPackageFile
{
    public string Path => targetPath;
    public string EffectivePath => targetPath;
    public System.Runtime.Versioning.FrameworkName TargetFramework => null!;
    public NuGet.Frameworks.NuGetFramework NuGetFramework => NuGet.Frameworks.NuGetFramework.AnyFramework;

    public DateTimeOffset LastWriteTime => DateTimeOffset.Now;

    public Stream GetStream()
    {
        stream.Position = 0;
        return stream;
    }
}
