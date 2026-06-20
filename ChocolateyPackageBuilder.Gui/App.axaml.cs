using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChocolateyPackageBuilder.Core;
using ChocolateyPackageBuilder.Gui.Services;
using ChocolateyPackageBuilder.Gui.ViewModels;
using ChocolateyPackageBuilder.Gui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ChocolateyPackageBuilder.Gui;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddChocolateyPackageBuilderCore();

            services.AddSingleton<AppStatusViewModel>();
            services.AddSingleton<IAppSettingsStore, AppSettingsStore>();
            services.AddSingleton<SettingsViewModel>();

            services.AddTransient<MainWindow>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<PackageBuilderViewModel>();

            var mainWindow = new MainWindow();
            services.AddSingleton<IFileDialogService>(new FileDialogService(mainWindow));

            var provider = services.BuildServiceProvider();
            mainWindow.DataContext = provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}