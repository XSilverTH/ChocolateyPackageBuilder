using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.Cli;

public static class Program
{
    public static int Main(string[] args)
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