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
public sealed record CustomProjectPackRequest(string ProjectPath, string OutputDirectory);

public sealed record PackCustomProjectResult(string OutputPath);


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

    public static PackScaffoldResult PackScaffold(string directoryPath, string? outputDirectory = null)
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
        var destinationDirectory = outputDirectory ?? parentDirectory;
        Directory.CreateDirectory(destinationDirectory);
        var outputPath = Path.Combine(destinationDirectory, $"{builder.Id}.{builder.Version}.nupkg");

        using var outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        builder.Save(outFs);

        return new PackScaffoldResult(outputPath);
    }

    public static async Task<PackCustomProjectResult> PackCustomProjectAsync(CustomProjectPackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfWhiteSpace(request.ProjectPath, nameof(request.ProjectPath));
        ThrowIfWhiteSpace(request.OutputDirectory, nameof(request.OutputDirectory));

        var project = await CustomInstallerProjectStore.LoadAsync(request.ProjectPath, cancellationToken);
        ThrowIfWhiteSpace(project.Package.Name, nameof(project.Package.Name));
        ThrowIfWhiteSpace(project.Package.Version, nameof(project.Package.Version));
        ThrowIfWhiteSpace(project.Package.Maintainer, nameof(project.Package.Maintainer));
        ThrowIfWhiteSpace(project.Package.Description, nameof(project.Package.Description));

        var version = new NuGetVersion(project.Package.Version);
        if (project.Actions.Count == 0)
            throw new InvalidOperationException("Custom installer project must contain at least one action.");

        var projectRoot = Path.GetDirectoryName(Path.GetFullPath(request.ProjectPath)) ?? Environment.CurrentDirectory;
        var resolvedFiles = ValidateProjectFiles(project, projectRoot);
        ValidateActions(project, resolvedFiles.Keys);

        Directory.CreateDirectory(request.OutputDirectory);
        var builder = new PackageBuilder
        {
            Id = project.Package.Name,
            Version = version,
            Description = project.Package.Description
        };
        builder.Authors.Add(project.Package.Maintainer);
        builder.Files.Add(new StreamPackageFile(
            new MemoryStream(Encoding.UTF8.GetBytes(CustomInstallerScriptGenerator.Generate(project))),
            @"tools\chocolateyInstall.ps1"));

        foreach (var file in resolvedFiles.Values)
        {
            builder.Files.Add(new PhysicalPackageFile
            {
                SourcePath = file.ResolvedSourcePath,
                TargetPath = $@"tools\{file.NormalizedPackagePath}"
            });
        }

        var outputPath = Path.Combine(request.OutputDirectory, $"{project.Package.Name}.{project.Package.Version}.nupkg");
        await Task.Run(() =>
        {
            using var outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            builder.Save(outFs);
        }, cancellationToken);

        return new PackCustomProjectResult(outputPath);
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

    private static Dictionary<string, ResolvedProjectFile> ValidateProjectFiles(CustomInstallerProject project,
        string projectRoot)
    {
        var files = new Dictionary<string, ResolvedProjectFile>(StringComparer.OrdinalIgnoreCase);
        var packagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fullProjectRoot = Path.GetFullPath(projectRoot);

        foreach (var file in project.Files)
        {
            if (string.IsNullOrWhiteSpace(file.Id))
                throw new InvalidOperationException("Project file id cannot be empty.");
            if (!files.TryAdd(file.Id, null!))
                throw new InvalidOperationException($"Project file id '{file.Id}' is duplicated.");

            var resolvedSourcePath = ResolveProjectSourcePath(fullProjectRoot, file);
            if (!File.Exists(resolvedSourcePath))
                throw new FileNotFoundException($"Project file not found: {resolvedSourcePath}", resolvedSourcePath);

            var normalizedPackagePath = NormalizePackagePath(file);
            if (!packagePaths.Add(normalizedPackagePath))
                throw new InvalidOperationException($"Project package path '{file.PackagePath}' is duplicated.");

            files[file.Id] = new ResolvedProjectFile(resolvedSourcePath, normalizedPackagePath);
        }

        return files;
    }

    private static string ResolveProjectSourcePath(string projectRoot, ProjectFile file)
    {
        if (string.IsNullOrWhiteSpace(file.SourcePath) || Path.IsPathRooted(file.SourcePath))
            throw new InvalidOperationException($"Project file '{file.Id}' must stay inside the project directory.");

        var resolvedPath = Path.GetFullPath(Path.Combine(projectRoot, file.SourcePath));
        var relativePath = Path.GetRelativePath(projectRoot, resolvedPath);
        if (relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath))
            throw new InvalidOperationException($"Project file '{file.Id}' must stay inside the project directory.");

        return resolvedPath;
    }

    private static string NormalizePackagePath(ProjectFile file)
    {
        if (string.IsNullOrWhiteSpace(file.PackagePath) || Path.IsPathRooted(file.PackagePath))
            throw new InvalidOperationException($"Project file '{file.Id}' package path must be a safe relative path.");

        var parts = file.PackagePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(part => part == ".."))
            throw new InvalidOperationException($"Project file '{file.Id}' package path must be a safe relative path.");

        return string.Join('\\', parts);
    }

    private static void ValidateActions(CustomInstallerProject project, IEnumerable<string> projectFileIds)
    {
        var ids = new HashSet<string>(projectFileIds, StringComparer.OrdinalIgnoreCase);
        foreach (var action in project.Actions)
        {
            switch (action.Kind)
            {
                case InstallActionKind.CopyFile:
                    ValidateActionPath(action.Source, ids);
                    ValidateActionPath(action.Destination, ids);
                    break;
                case InstallActionKind.RunFile:
                    ValidateActionPath(action.File, ids);
                    if (action.ValidExitCodes.Count == 0)
                        action.ValidExitCodes.Add(0);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported install action kind: {action.Kind}.");
            }
        }
    }

    private static void ValidateActionPath(ActionPath path, HashSet<string> projectFileIds)
    {
        if (path.Kind == ActionPathKind.PackageFile)
        {
            if (!projectFileIds.Contains(path.Value))
                throw new InvalidOperationException($"Unknown project file id: {path.Value}");
            return;
        }

        if (path.Kind == ActionPathKind.Literal)
        {
            if (string.IsNullOrWhiteSpace(path.Value))
                throw new InvalidOperationException("Literal action path cannot be empty.");
            return;
        }

        throw new InvalidOperationException($"Unsupported action path kind: {path.Kind}.");
    }

    private sealed record ResolvedProjectFile(string ResolvedSourcePath, string NormalizedPackagePath);

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