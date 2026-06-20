namespace ChocolateyPackageBuilder.Core.Models;

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