namespace ChocolateyPackageBuilder.Core.Models;

public sealed class ProjectFile
{
    public string Id { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string PackagePath { get; set; } = string.Empty;
}