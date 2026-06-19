using System.Threading.Tasks;

namespace ChocolateyPackageBuilder.App.Services;

public interface IFileDialogService
{
    Task<string?> PickInstallerAsync();
    Task<string?> PickOutputDirectoryAsync();
}