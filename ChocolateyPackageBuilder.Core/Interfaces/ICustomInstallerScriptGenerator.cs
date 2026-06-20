using ChocolateyPackageBuilder.Core.Models;

namespace ChocolateyPackageBuilder.Core.Interfaces;

public interface ICustomInstallerScriptGenerator
{
    string Generate(CustomInstallerProject project);
}