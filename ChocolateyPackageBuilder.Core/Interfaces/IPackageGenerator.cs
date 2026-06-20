namespace ChocolateyPackageBuilder.Core.Interfaces;

public interface IPackageGenerator
{
    Task<PackageBuildResult> GenerateAsync(PackageBuildRequest request);
    PackScaffoldResult PackScaffold(string directoryPath, string? outputDirectory = null);

    Task<PackCustomProjectResult> PackCustomProjectAsync(CustomProjectPackRequest request,
        CancellationToken cancellationToken = default);
}