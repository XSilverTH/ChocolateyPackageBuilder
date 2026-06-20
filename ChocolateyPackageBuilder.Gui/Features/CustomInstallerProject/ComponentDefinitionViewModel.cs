using ChocolateyPackageBuilder.Core;

namespace ChocolateyPackageBuilder.Gui.Features.CustomInstallerProject;

public sealed class ComponentDefinitionViewModel(InstallActionKind kind, string title, string description)
{
    public InstallActionKind Kind { get; } = kind;
    public string Title { get; } = title;
    public string Description { get; } = description;
}
