using ChocolateyPackageBuilder.Gui.Features.PackageBuilder;
using ChocolateyPackageBuilder.Gui.Services;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(IFileDialogService fileDialogService)
    {
        PackageBuilder = new PackageBuilderViewModel(fileDialogService);
    }

    public PackageBuilderViewModel PackageBuilder { get; }
}