using ChocolateyPackageBuilder.Core.Utilities;

namespace ChocolateyPackageBuilder.Core.Models;

public sealed class PackageMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Maintainer { get; set; } = PackageUtility.DefaultMaintainer();
    public string Description { get; set; } = string.Empty;
}