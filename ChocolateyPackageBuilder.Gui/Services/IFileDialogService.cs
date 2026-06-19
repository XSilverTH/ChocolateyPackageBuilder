using System.Threading.Tasks;

namespace ChocolateyPackageBuilder.Gui.Services;

public interface IFileDialogService
{
    Task<string?> PickInstallerAsync();
    Task<string?> PickOutputDirectoryAsync();
}