using ChocolateyPackageBuilder.Cli.Commands;
using ChocolateyPackageBuilder.Cli.Infrastructure;
using ChocolateyPackageBuilder.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace ChocolateyPackageBuilder.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddChocolateyPackageBuilderCore();

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);
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