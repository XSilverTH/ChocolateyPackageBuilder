using Spectre.Console.Cli;
using ChocolateyPackageBuilder.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("ChocolateyPackageBuilder");

    config.AddCommand<BuildCommand>("build")
        .WithDescription("Builds a Chocolatey package from an installer.");

    config.AddCommand<PackCommand>("pack")
        .WithDescription("Packs a scaffolded template directory into a .nupkg.");
});

return app.Run(args);