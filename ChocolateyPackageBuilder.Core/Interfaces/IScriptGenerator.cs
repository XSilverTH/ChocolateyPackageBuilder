using ChocolateyPackageBuilder.Core.Services;

namespace ChocolateyPackageBuilder.Core.Interfaces;

public interface IScriptGenerator
{
    string Generate(InstallerType type, string packageName, string installerFileName);
}