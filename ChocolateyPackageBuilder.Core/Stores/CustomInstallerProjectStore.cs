using System.Text.Json;
using System.Text.Json.Serialization;
using ChocolateyPackageBuilder.Core.Interfaces;
using ChocolateyPackageBuilder.Core.Models;

namespace ChocolateyPackageBuilder.Core.Stores;

public sealed class CustomInstallerProjectStore : ICustomInstallerProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FileExtension => ".cpbproj";

    public async Task<CustomInstallerProject> LoadAsync(string projectPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(projectPath))
            throw new FileNotFoundException($"Project file not found: {projectPath}", projectPath);

        await using var stream = new FileStream(projectPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
            true);
        var project =
            await JsonSerializer.DeserializeAsync<CustomInstallerProject>(stream, JsonOptions, cancellationToken) ??
            throw new InvalidOperationException("Project file is empty.");

        if (project.SchemaVersion != 1)
            throw new InvalidOperationException($"Unsupported project schema version: {project.SchemaVersion}.");

        return project;
    }

    public async Task SaveAsync(string projectPath, CustomInstallerProject project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        var directory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var stream = new FileStream(projectPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920,
            true);
        await JsonSerializer.SerializeAsync(stream, project, JsonOptions, cancellationToken);
    }
}