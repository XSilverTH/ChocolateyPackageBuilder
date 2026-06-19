using ChocolateyPackageBuilder.Gui.Features.CustomInstallerProject;
using ChocolateyPackageBuilder.Gui.Features.PackageBuilder;
using ChocolateyPackageBuilder.Gui.Services;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(IFileDialogService fileDialogService)
    {
        CustomInstallerProject = new CustomInstallerProjectViewModel(fileDialogService);
        PackageBuilder = new PackageBuilderViewModel(fileDialogService);
    }

    public CustomInstallerProjectViewModel CustomInstallerProject { get; }
    public PackageBuilderViewModel PackageBuilder { get; }
}