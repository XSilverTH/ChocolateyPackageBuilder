namespace ChocolateyPackageBuilder.Core.Models;

public sealed class CustomInstallerProject
{
    public int SchemaVersion { get; init; } = 1;
    public PackageMetadata Package { get; init; } = new();
    public List<ProjectFile> Files { get; init; } = [];
    public List<InstallAction> Actions { get; init; } = [];
}