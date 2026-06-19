using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.App.Cli;

internal static class CliApplication
{
    public static int Run(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("ChocolateyPackageBuilder");
            config.AddCommand<BuildCommand>("build")
                .WithDescription("Build a Chocolatey package or scaffold from an installer.");
            config.AddCommand<PackCommand>("pack")
                .WithDescription("Pack a scaffolded template directory into a .nupkg.");
        });

        return app.Run(args);
    }
}
