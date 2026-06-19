using System;
using Avalonia;
using ChocolateyPackageBuilder.Gui;

namespace ChocolateyPackageBuilder;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        return args.Length > 0 ? Cli.Program.Main(args) : BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
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