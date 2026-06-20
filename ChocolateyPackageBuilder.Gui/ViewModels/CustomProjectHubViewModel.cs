using CommunityToolkit.Mvvm.Input;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public partial class CustomProjectHubViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;

    public CustomProjectHubViewModel(MainWindowViewModel mainWindow)
    {
        _mainWindow = mainWindow;
    }

    [RelayCommand]
    private void NewProject()
    {
        _mainWindow.NewCustomProjectCommand.Execute(null);
    }

    [RelayCommand]
    private void OpenProject()
    {
        _mainWindow.OpenCustomProjectCommand.Execute(null);
    }
}