using System;
using Avalonia;
using ChocolateyPackageBuilder.App.Cli;

namespace ChocolateyPackageBuilder.App;

internal sealed class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        return args.Length > 0 ? CliApplication.Run(args) : BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}