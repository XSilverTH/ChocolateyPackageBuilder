using ChocolateyPackageBuilder.App.Features.PackageBuilder;
using ChocolateyPackageBuilder.App.Services;

namespace ChocolateyPackageBuilder.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(IFileDialogService fileDialogService)
    {
        PackageBuilder = new PackageBuilderViewModel(fileDialogService);
    }

    public PackageBuilderViewModel PackageBuilder { get; }
}
