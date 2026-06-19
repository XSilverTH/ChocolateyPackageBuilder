using System.Runtime.Versioning;
using System.Text;
using System.Xml.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace ChocolateyPackageBuilder.Core;

public sealed record PackageBuildRequest(
    string InstallerPath,
    InstallerType InstallerType,
    string PackageName,
    string Version,
    string Maintainer,
    string Description,
    string OutputDirectory);

public sealed record PackageBuildResult(string OutputPath, bool IsScaffold);

public sealed record PackScaffoldResult(string OutputPath);

public static class PackageGenerator
{
    public static async Task<PackageBuildResult> GenerateAsync(PackageBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        if (!File.Exists(request.InstallerPath))
            throw new FileNotFoundException($"Installer not found: {request.InstallerPath}", request.InstallerPath);

        Directory.CreateDirectory(request.OutputDirectory);

        var installerFileName = Path.GetFileName(request.InstallerPath);
        var scriptContent = ScriptGenerator.Generate(request.InstallerType, request.PackageName, installerFileName);

        return request.InstallerType == InstallerType.Unknown
            ? await GenerateScaffoldAsync(request, installerFileName, scriptContent)
            : await GeneratePackageAsync(request, installerFileName, scriptContent);
    }

    public static PackScaffoldResult PackScaffold(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var nuspecFiles = Directory.GetFiles(directoryPath, "*.nuspec");
        if (nuspecFiles.Length != 1)
            throw new InvalidOperationException(
                $"Expected exactly one .nuspec file in '{directoryPath}', found {nuspecFiles.Length}.");

        using var fs = new FileStream(nuspecFiles[0], FileMode.Open, FileAccess.Read, FileShare.Read);
        var builder = new PackageBuilder(fs, directoryPath);
        var parentDirectory = Directory.GetParent(Path.GetFullPath(directoryPath))?.FullName ??
                              Environment.CurrentDirectory;
        var outputPath = Path.Combine(parentDirectory, $"{builder.Id}.{builder.Version}.nupkg");

        using var outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        builder.Save(outFs);

        return new PackScaffoldResult(outputPath);
    }

    private static async Task<PackageBuildResult> GeneratePackageAsync(PackageBuildRequest request, string installerFileName,
        string scriptContent)
    {
        var builder = new PackageBuilder
        {
            Id = request.PackageName,
            Version = new NuGetVersion(request.Version),
            Description = request.Description
        };
        builder.Authors.Add(request.Maintainer);
        builder.Files.Add(new StreamPackageFile(new MemoryStream(Encoding.UTF8.GetBytes(scriptContent)),
            @"tools\chocolateyInstall.ps1"));
        builder.Files.Add(new PhysicalPackageFile
        {
            SourcePath = request.InstallerPath,
            TargetPath = $@"tools\{installerFileName}"
        });

        var outputPath = Path.Combine(request.OutputDirectory, $"{request.PackageName}.{request.Version}.nupkg");
        await Task.Run(() =>
        {
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            builder.Save(fs);
        });

        return new PackageBuildResult(outputPath, false);
    }

    private static async Task<PackageBuildResult> GenerateScaffoldAsync(PackageBuildRequest request, string installerFileName,
        string scriptContent)
    {
        _ = new NuGetVersion(request.Version);

        var templateDir = Path.Combine(request.OutputDirectory, $"{request.PackageName}-template");
        var toolsDir = Path.Combine(templateDir, "tools");
        Directory.CreateDirectory(toolsDir);

        var destinationPath = Path.Combine(toolsDir, installerFileName);
        
        using (var sourceStream = new FileStream(request.InstallerPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
        using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }

        await File.WriteAllTextAsync(Path.Combine(toolsDir, "chocolateyInstall.ps1"), scriptContent, Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(templateDir, $"{request.PackageName}.nuspec"), CreateNuspec(request),
            Encoding.UTF8);

        return new PackageBuildResult(templateDir, true);
    }

    private static string CreateNuspec(PackageBuildRequest request)
    {
        XNamespace ns = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "package",
                new XElement(ns + "metadata",
                    new XElement(ns + "id", request.PackageName),
                    new XElement(ns + "version", request.Version),
                    new XElement(ns + "authors", request.Maintainer),
                    new XElement(ns + "description", request.Description))));

        return document + Environment.NewLine;
    }

    private static void ValidateRequest(PackageBuildRequest request)
    {
        ThrowIfWhiteSpace(request.InstallerPath, nameof(request.InstallerPath));
        ThrowIfWhiteSpace(request.PackageName, nameof(request.PackageName));
        ThrowIfWhiteSpace(request.Version, nameof(request.Version));
        ThrowIfWhiteSpace(request.Maintainer, nameof(request.Maintainer));
        ThrowIfWhiteSpace(request.Description, nameof(request.Description));
        ThrowIfWhiteSpace(request.OutputDirectory, nameof(request.OutputDirectory));
    }

    private static void ThrowIfWhiteSpace(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{fieldName} cannot be empty.", fieldName);
    }
}

public sealed class StreamPackageFile(Stream stream, string targetPath) : IPackageFile
{
    public string Path => targetPath;
    public string EffectivePath => targetPath;
    public FrameworkName TargetFramework => null!;
    public NuGetFramework NuGetFramework => NuGetFramework.AnyFramework;
    public DateTimeOffset LastWriteTime => DateTimeOffset.Now;

    public Stream GetStream()
    {
        stream.Position = 0;
        return stream;
    }
}