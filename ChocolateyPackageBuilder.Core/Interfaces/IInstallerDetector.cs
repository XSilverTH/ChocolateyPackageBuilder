using ChocolateyPackageBuilder.Core.Services;

namespace ChocolateyPackageBuilder.Core.Interfaces;

public interface IInstallerDetector
{
    InstallerType Detect(string filePath);
}