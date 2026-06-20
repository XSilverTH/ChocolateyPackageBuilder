using Avalonia.Input;
using ChocolateyPackageBuilder.Gui.ViewModels;
using SukiUI.Controls;

namespace ChocolateyPackageBuilder.Gui.Views;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void QuickInstaller_Tapped(object? sender, TappedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).ShowQuickInstallerCommand.Execute(null);
    }

    private void CurrentProject_Tapped(object? sender, TappedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).ShowCustomProjectCommand.Execute(null);
    }
}