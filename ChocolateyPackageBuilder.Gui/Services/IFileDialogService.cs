using System.Threading.Tasks;

namespace ChocolateyPackageBuilder.Gui.Services;

public interface IFileDialogService
{
    Task<string?> PickInstallerAsync();
    Task<string?> PickOutputDirectoryAsync();
    Task<string?> PickProjectFileAsync();
    Task<string?> SaveProjectFileAsync();
    Task<string?> PickAnyFileAsync();
}