using ChocolateyPackageBuilder.Core.Models;

namespace ChocolateyPackageBuilder.Core.Interfaces;

public interface ICustomInstallerProjectStore
{
    string FileExtension { get; }
    Task<CustomInstallerProject> LoadAsync(string path, CancellationToken cancellationToken = default);
    Task SaveAsync(string path, CustomInstallerProject project, CancellationToken cancellationToken = default);
}