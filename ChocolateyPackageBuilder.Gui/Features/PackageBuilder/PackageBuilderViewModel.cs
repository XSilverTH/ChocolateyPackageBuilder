using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChocolateyPackageBuilder.Core;
using ChocolateyPackageBuilder.Gui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateyPackageBuilder.Gui.Features.PackageBuilder;

public partial class PackageBuilderViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    private string description = string.Empty;

    [ObservableProperty] private InstallerType detectedInstallerType = InstallerType.Unknown;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DetectInstallerCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    private string installerPath = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    private bool isBusy;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    private string maintainer = PackageUtility.DefaultMaintainer();

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    private string outputDirectory = Environment.CurrentDirectory;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    private string packageName = string.Empty;

    [ObservableProperty]
    private string scriptPreview = "Select an installer to preview the generated Chocolatey install script.";

    [ObservableProperty] private InstallerType selectedInstallerType = InstallerType.Unknown;

    [ObservableProperty] private string statusMessage = "Ready.";

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    private string version = "1.0.0";

    public PackageBuilderViewModel(IFileDialogService fileDialogService)
    {
        _fileDialogService = fileDialogService;
    }

    public IReadOnlyList<InstallerType> InstallerTypes { get; } =
    [
        InstallerType.Msi,
        InstallerType.InnoSetup,
        InstallerType.Nsis,
        InstallerType.Unknown
    ];

    [RelayCommand]
    private async Task BrowseInstallerAsync()
    {
        var path = await _fileDialogService.PickInstallerAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        InstallerPath = path;
        if (string.IsNullOrWhiteSpace(PackageName))
            PackageName = PackageUtility.CreatePackageSlug(Path.GetFileNameWithoutExtension(path));

        if (string.IsNullOrWhiteSpace(Description)) Description = $"Chocolatey package for {PackageName}.";

        DetectInstaller();
    }

    [RelayCommand]
    private async Task BrowseOutputDirectoryAsync()
    {
        var path = await _fileDialogService.PickOutputDirectoryAsync();
        if (!string.IsNullOrWhiteSpace(path)) OutputDirectory = path;
    }

    [RelayCommand(CanExecute = nameof(CanDetectInstaller))]
    private void DetectInstaller()
    {
        try
        {
            DetectedInstallerType = InstallerDetector.Detect(InstallerPath);
            SelectedInstallerType = DetectedInstallerType;
            StatusMessage = DetectedInstallerType == InstallerType.Unknown
                ? "Installer type was not detected. The scaffold script needs review."
                : $"Detected {DetectedInstallerType}.";
            RefreshScriptPreview();
        }
        catch (Exception ex)
        {
            DetectedInstallerType = InstallerType.Unknown;
            SelectedInstallerType = InstallerType.Unknown;
            ScriptPreview = string.Empty;
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanBuildPackage))]
    private async Task BuildPackageAsync()
    {
        if (!ValidateForBuild()) return;

        IsBusy = true;
        StatusMessage = "Building package...";

        try
        {
            var request = new PackageBuildRequest(
                InstallerPath,
                SelectedInstallerType,
                PackageName.Trim(),
                Version.Trim(),
                Maintainer.Trim(),
                Description.Trim(),
                OutputDirectory.Trim());

            var result = await PackageGenerator.GenerateAsync(request);
            StatusMessage = result.IsScaffold
                ? $"Scaffolded template: {result.OutputPath}. Review tools/chocolateyInstall.ps1 before packing."
                : $"Built package: {result.OutputPath}";
            RefreshScriptPreview();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedInstallerTypeChanged(InstallerType value)
    {
        RefreshScriptPreview();
    }

    partial void OnPackageNameChanged(string? oldValue, string newValue)
    {
        var oldDefaultDescription = !string.IsNullOrWhiteSpace(oldValue)
            ? $"Chocolatey package for {oldValue.Trim()}."
            : string.Empty;

        if (string.IsNullOrWhiteSpace(Description) || Description == oldDefaultDescription)
        {
            if (!string.IsNullOrWhiteSpace(newValue))
            {
                Description = $"Chocolatey package for {newValue.Trim()}.";
            }
            else
            {
                Description = string.Empty;
            }
        }

        RefreshScriptPreview();
    }

    partial void OnInstallerPathChanged(string value)
    {
        RefreshScriptPreview();
    }

    private bool CanDetectInstaller()
    {
        return !string.IsNullOrWhiteSpace(InstallerPath) && File.Exists(InstallerPath);
    }

    private bool CanBuildPackage()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(InstallerPath) &&
               File.Exists(InstallerPath) &&
               !string.IsNullOrWhiteSpace(OutputDirectory);
    }

    private bool ValidateForBuild()
    {
        if (string.IsNullOrWhiteSpace(InstallerPath) || !File.Exists(InstallerPath))
        {
            StatusMessage = "Select an existing installer before building.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            StatusMessage = "Choose an output directory before building.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(PackageName))
        {
            StatusMessage = "Package name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            StatusMessage = "Version is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Maintainer))
        {
            StatusMessage = "Maintainer is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            StatusMessage = "Description is required.";
            return false;
        }

        return true;
    }

    private void RefreshScriptPreview()
    {
        if (string.IsNullOrWhiteSpace(InstallerPath) || string.IsNullOrWhiteSpace(PackageName))
        {
            ScriptPreview = "Select an installer and package name to preview the generated Chocolatey install script.";
            return;
        }

        var installerFileName = Path.GetFileName(InstallerPath);
        ScriptPreview = ScriptGenerator.Generate(SelectedInstallerType, PackageName.Trim(), installerFileName);
    }
}