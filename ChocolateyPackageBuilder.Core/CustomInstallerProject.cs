namespace ChocolateyPackageBuilder.Core;

public sealed class CustomInstallerProject
{
    public int SchemaVersion { get; init; } = 1;
    public PackageMetadata Package { get; init; } = new();
    public List<ProjectFile> Files { get; init; } = [];
    public List<InstallAction> Actions { get; init; } = [];
}

public sealed class PackageMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Maintainer { get; set; } = PackageUtility.DefaultMaintainer();
    public string Description { get; set; } = string.Empty;
}

public sealed class ProjectFile
{
    public string Id { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string PackagePath { get; set; } = string.Empty;
}

public sealed class InstallAction
{
    public InstallActionKind Kind { get; set; }
    public ActionPath Source { get; set; } = new();
    public ActionPath Destination { get; set; } = new();
    public ActionPath File { get; set; } = new();
    public string Arguments { get; set; } = string.Empty;
    public bool WaitForExit { get; set; } = true;
    public List<int> ValidExitCodes { get; set; } = [0];
    public bool Overwrite { get; set; } = true;
}

public enum InstallActionKind
{
    CopyFile,
    RunFile
}

public sealed class ActionPath
{
    public ActionPathKind Kind { get; set; } = ActionPathKind.Literal;
    public string Value { get; set; } = string.Empty;
}

public enum ActionPathKind
{
    PackageFile,
    Literal
}
