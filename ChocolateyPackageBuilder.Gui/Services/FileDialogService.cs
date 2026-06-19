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

    public async Task<string?> PickProjectFileAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open custom installer project",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Chocolatey Package Builder projects")
                {
                    Patterns = ["*.cpbproj"]
                },
                FilePickerFileTypes.All
            ]
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> SaveProjectFileAsync()
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save custom installer project",
            DefaultExtension = "cpbproj",
            FileTypeChoices =
            [
                new FilePickerFileType("Chocolatey Package Builder projects")
                {
                    Patterns = ["*.cpbproj"]
                }
            ]
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickAnyFileAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Add project file",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.All]
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