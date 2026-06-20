using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChocolateyPackageBuilder.Core;
using ChocolateyPackageBuilder.Core.Interfaces;
using ChocolateyPackageBuilder.Core.Services;
using ChocolateyPackageBuilder.Core.Utilities;
using ChocolateyPackageBuilder.Gui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public partial class PackageBuilderViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IInstallerDetector _installerDetector;
    private readonly IPackageGenerator _packageGenerator;
    private readonly IScriptGenerator _scriptGenerator;
    private readonly SettingsViewModel _settings;
    private readonly AppStatusViewModel _status;

    public PackageBuilderViewModel(
        IFileDialogService fileDialogService,
        AppStatusViewModel status,
        SettingsViewModel settings,
        IPackageGenerator packageGenerator,
        IInstallerDetector installerDetector,
        IScriptGenerator scriptGenerator)
    {
        _fileDialogService = fileDialogService;
        _status = status;
        _settings = settings;
        _packageGenerator = packageGenerator;
        _installerDetector = installerDetector;
        _scriptGenerator = scriptGenerator;
        IsScriptPreviewExpanded = !settings.RememberWorkspaceLayout || settings.ScriptPreviewExpanded;
        Steps =
        [
            new WizardStepItemViewModel("Installer", "Choose a package source.", 0),
            new WizardStepItemViewModel("Metadata", "Name the Chocolatey package.", 1),
            new WizardStepItemViewModel("Output", "Choose where to write output.", 2),
            new WizardStepItemViewModel("Review", "Build or scaffold the package.", 3)
        ];
        SetCurrentStep(0);
    }

    [ObservableProperty] private partial int CurrentStepIndex { get; set; }

    [ObservableProperty]
    public partial string CurrentStepSubtitle { get; set; } = "Choose the installer and silent-install strategy.";

    [ObservableProperty] public partial string CurrentStepTitle { get; set; } = "Installer";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty] public partial InstallerType DetectedInstallerType { get; set; } = InstallerType.Unknown;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DetectInstallerCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    public partial string InstallerPath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    private partial bool IsBusy { get; set; }

    [ObservableProperty] public partial bool IsInstallerStep { get; set; } = true;

    [ObservableProperty] public partial bool IsMetadataStep { get; set; }

    [ObservableProperty] public partial bool IsOutputStep { get; set; }

    [ObservableProperty] public partial bool IsReviewStep { get; set; }

    [ObservableProperty] public partial bool IsScriptPreviewExpanded { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    public partial string Maintainer { get; set; } = PackageUtility.DefaultMaintainer();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    public partial string OutputDirectory { get; set; } = Environment.CurrentDirectory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    public partial string PackageName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScriptPreview { get; set; } =
        "Select an installer to preview the generated Chocolatey install script.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    public partial InstallerType SelectedInstallerType { get; set; } = InstallerType.Unknown;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildPackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToStepCommand))]
    public partial string Version { get; set; } = "1.0.0";

    public IReadOnlyList<WizardStepItemViewModel> Steps { get; }

    public IReadOnlyList<InstallerType> InstallerTypes { get; } =
        [InstallerType.Msi, InstallerType.InnoSetup, InstallerType.Nsis, InstallerType.Unknown];

    public bool CanGoBack => CurrentStepIndex > 0 && !IsBusy;
    public bool CanGoNext => CurrentStepIndex < Steps.Count - 1 && IsStepValid(CurrentStepIndex) && !IsBusy;
    public bool IsNotReviewStep => !IsReviewStep;
    public bool IsScriptPreviewCollapsed => !IsScriptPreviewExpanded;


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
            DetectedInstallerType = _installerDetector.Detect(InstallerPath);
            SelectedInstallerType = DetectedInstallerType;
            _status.SetSuccess(DetectedInstallerType == InstallerType.Unknown
                ? "Installer type not detected; scaffold script needs review."
                : $"Detected {DetectedInstallerType}.");
            RefreshScriptPreview();
        }
        catch (Exception ex)
        {
            DetectedInstallerType = InstallerType.Unknown;
            SelectedInstallerType = InstallerType.Unknown;
            if (!CanDetectInstaller()) ScriptPreview = string.Empty;
            _status.SetError(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextStep()
    {
        SetCurrentStep(Math.Min(CurrentStepIndex + 1, Steps.Count - 1));
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void PreviousStep()
    {
        SetCurrentStep(Math.Max(CurrentStepIndex - 1, 0));
    }

    [RelayCommand(CanExecute = nameof(CanGoToStep))]
    private void GoToStep(WizardStepItemViewModel? step)
    {
        if (step is null) return;
        SetCurrentStep(step.Index);
    }

    private bool CanGoToStep(WizardStepItemViewModel? step)
    {
        if (step is null) return false;
        return step.Index <= CurrentStepIndex || (step.Index == CurrentStepIndex + 1 && AllPriorStepsValid(step.Index));
    }

    [RelayCommand(CanExecute = nameof(CanBuildPackage))]
    private async Task BuildPackageAsync()
    {
        if (!ValidateForBuild()) return;

        IsBusy = true;
        _status.SetBusy("Building package...", _settings.VerboseStatus ? OutputDirectory.Trim() : string.Empty);

        try
        {
            var request = new PackageBuildRequest(InstallerPath, SelectedInstallerType, PackageName.Trim(),
                Version.Trim(), Maintainer.Trim(), Description.Trim(), OutputDirectory.Trim());
            var result = await _packageGenerator.GenerateAsync(request);
            _status.SetSuccess(result.IsScaffold
                ? $"Scaffolded template: {result.OutputPath}. Review tools/chocolateyInstall.ps1 before packing."
                : $"Built package: {result.OutputPath}");
            RefreshScriptPreview();
        }
        catch (Exception ex)
        {
            _status.SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleScriptPreview()
    {
        IsScriptPreviewExpanded = !IsScriptPreviewExpanded;
        if (_settings.RememberWorkspaceLayout) _settings.ScriptPreviewExpanded = IsScriptPreviewExpanded;
    }

    partial void OnIsReviewStepChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotReviewStep));
    }

    partial void OnIsScriptPreviewExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsScriptPreviewCollapsed));
    }


    partial void OnCurrentStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        GoToStepCommand.NotifyCanExecuteChanged();
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
            Description = string.IsNullOrWhiteSpace(newValue)
                ? string.Empty
                : $"Chocolatey package for {newValue.Trim()}.";
        RefreshScriptPreview();
    }

    partial void OnInstallerPathChanged(string value)
    {
        RefreshScriptPreview();
    }

    partial void OnDescriptionChanged(string value)
    {
        NotifyStepState();
    }

    partial void OnMaintainerChanged(string value)
    {
        NotifyStepState();
    }

    partial void OnOutputDirectoryChanged(string value)
    {
        NotifyStepState();
    }

    partial void OnVersionChanged(string value)
    {
        NotifyStepState();
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyStepState();
    }

    private bool CanDetectInstaller()
    {
        return !string.IsNullOrWhiteSpace(InstallerPath) && File.Exists(InstallerPath);
    }

    private bool CanBuildPackage()
    {
        return IsReviewStep && !IsBusy && IsStepValid(3);
    }

    private bool ValidateForBuild()
    {
        if (!IsStepValid(0)) return Invalid(0, "Select an existing installer before building.");
        if (string.IsNullOrWhiteSpace(OutputDirectory))
            return Invalid(2, "Choose an output directory before building.");
        if (string.IsNullOrWhiteSpace(PackageName)) return Invalid(1, "Package name is required.");
        if (string.IsNullOrWhiteSpace(Version)) return Invalid(1, "Version is required.");
        if (string.IsNullOrWhiteSpace(Maintainer)) return Invalid(1, "Maintainer is required.");
        if (string.IsNullOrWhiteSpace(Description)) return Invalid(1, "Description is required.");
        return true;
    }

    private bool Invalid(int step, string message)
    {
        _status.SetError(message);
        SetCurrentStep(step);
        return false;
    }

    private void SetCurrentStep(int index)
    {
        CurrentStepIndex = Math.Clamp(index, 0, Steps.Count - 1);
        var current = Steps[CurrentStepIndex];
        CurrentStepTitle = current.Title;
        CurrentStepSubtitle = current.Subtitle;
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsCurrent = i == CurrentStepIndex;
            Steps[i].IsComplete = i < CurrentStepIndex && IsStepValid(i);
        }

        IsInstallerStep = CurrentStepIndex == 0;
        IsMetadataStep = CurrentStepIndex == 1;
        IsOutputStep = CurrentStepIndex == 2;
        IsReviewStep = CurrentStepIndex == 3;
        NotifyStepState();
    }

    private void NotifyStepState()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        NextStepCommand.NotifyCanExecuteChanged();
        PreviousStepCommand.NotifyCanExecuteChanged();
        BuildPackageCommand.NotifyCanExecuteChanged();
        GoToStepCommand.NotifyCanExecuteChanged();
    }

    private bool AllPriorStepsValid(int index)
    {
        for (var i = 0; i < index; i++)
            if (!IsStepValid(i))
                return false;
        return true;
    }

    private bool IsStepValid(int index)
    {
        return index switch
        {
            0 => !string.IsNullOrWhiteSpace(InstallerPath) && File.Exists(InstallerPath),
            1 => !string.IsNullOrWhiteSpace(PackageName) && !string.IsNullOrWhiteSpace(Version) &&
                 !string.IsNullOrWhiteSpace(Maintainer) && !string.IsNullOrWhiteSpace(Description),
            2 => !string.IsNullOrWhiteSpace(OutputDirectory),
            3 => AllPriorStepsValid(3) && !IsBusy,
            _ => false
        };
    }

    private void RefreshScriptPreview()
    {
        if (string.IsNullOrWhiteSpace(InstallerPath) || string.IsNullOrWhiteSpace(PackageName))
        {
            ScriptPreview = "Select an installer and package name to preview the generated Chocolatey install script.";
            return;
        }

        ScriptPreview =
            _scriptGenerator.Generate(SelectedInstallerType, PackageName.Trim(), Path.GetFileName(InstallerPath));
    }
}