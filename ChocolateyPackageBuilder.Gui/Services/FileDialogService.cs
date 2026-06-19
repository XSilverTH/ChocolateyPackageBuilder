using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ChocolateyPackageBuilder.Gui.Services;

public sealed class FileDialogService(Window owner) : IFileDialogService
{
    public async Task<string?> PickInstallerAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select installer",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Installers")
                {
                    Patterns = ["*.exe", "*.msi"]
                },
                FilePickerFileTypes.All
            ]
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> PickOutputDirectoryAsync()
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select output directory",
            AllowMultiple = false
        });

        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }
}