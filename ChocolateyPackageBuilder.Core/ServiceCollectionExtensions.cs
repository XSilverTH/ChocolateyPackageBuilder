using ChocolateyPackageBuilder.Core.Generators;
using ChocolateyPackageBuilder.Core.Interfaces;
using ChocolateyPackageBuilder.Core.Services;
using ChocolateyPackageBuilder.Core.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace ChocolateyPackageBuilder.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChocolateyPackageBuilderCore(this IServiceCollection services)
    {
        services.AddTransient<IScriptGenerator, ScriptGenerator>();
        services.AddTransient<ICustomInstallerScriptGenerator, CustomInstallerScriptGenerator>();
        services.AddTransient<ICustomInstallerProjectStore, CustomInstallerProjectStore>();
        services.AddTransient<IInstallerDetector, InstallerDetector>();
        services.AddTransient<IPackageGenerator, PackageGenerator>();
        return services;
    }
}