using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChocolateyPackageBuilder.Core;
using ChocolateyPackageBuilder.Gui.Services;
using ChocolateyPackageBuilder.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateyPackageBuilder.Gui.Features.CustomInstallerProject;

public sealed partial class CustomProjectSetupViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly Action<ChocolateyPackageBuilder.Core.CustomInstallerProject, string, string> _openWorkspace;
    private readonly AppStatusViewModel _status;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string description = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private bool isBusy;

    [ObservableProperty] private bool isCreateStep;
    [ObservableProperty] private bool isMetadataStep;
    [ObservableProperty] private bool isOutputStep;
    [ObservableProperty] private bool isProjectStep = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string maintainer = PackageUtility.DefaultMaintainer();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string outputDirectory = Environment.CurrentDirectory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string packageName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string projectPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousStepCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string version = "1.0.0";

    [ObservableProperty] private int currentStepIndex;
    [ObservableProperty] private string currentStepTitle = "Project";
    [ObservableProperty] private string currentStepSubtitle = "Choose where the .cpbproj file will live.";

    public CustomProjectSetupViewModel(IFileDialogService fileDialogService, AppStatusViewModel status, SettingsViewModel settings, Action<ChocolateyPackageBuilder.Core.CustomInstallerProject, string, string> openWorkspace)
    {
        _fileDialogService = fileDialogService;
        _status = status;
        _openWorkspace = openWorkspace;
        Steps =
        [
            new WizardStepItemViewModel("Project", "Choose a project file.", 0),
            new WizardStepItemViewModel("Metadata", "Name the package.", 1),
            new WizardStepItemViewModel("Output", "Choose session output.", 2),
            new WizardStepItemViewModel("Create", "Save and open the workspace.", 3)
        ];
        SetCurrentStep(0);
    }

    public IReadOnlyList<WizardStepItemViewModel> Steps { get; }
    public bool CanGoBack => CurrentStepIndex > 0 && !IsBusy;
    public bool CanGoNext => CurrentStepIndex < Steps.Count - 1 && IsStepValid(CurrentStepIndex) && !IsBusy;

    [RelayCommand]
    private async Task BrowseProjectPathAsync()
    {
        var path = await _fileDialogService.SaveProjectFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;
        ProjectPath = Path.GetExtension(path).Equals(CustomInstallerProjectStore.FileExtension, StringComparison.OrdinalIgnoreCase)
            ? path
            : path + CustomInstallerProjectStore.FileExtension;
    }

    [RelayCommand]
    private async Task BrowseOutputDirectoryAsync()
    {
        var path = await _fileDialogService.PickOutputDirectoryAsync();
        if (!string.IsNullOrWhiteSpace(path)) OutputDirectory = path;
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextStep() => SetCurrentStep(Math.Min(CurrentStepIndex + 1, Steps.Count - 1));

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void PreviousStep() => SetCurrentStep(Math.Max(CurrentStepIndex - 1, 0));

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task CreateProjectAsync()
    {
        if (!IsStepValid(3)) return;
        IsBusy = true;
        try
        {
            var project = new ChocolateyPackageBuilder.Core.CustomInstallerProject
            {
                Package = new PackageMetadata
                {
                    Name = PackageName.Trim(),
                    Version = Version.Trim(),
                    Maintainer = Maintainer.Trim(),
                    Description = Description.Trim()
                },
                Files = [],
                Actions = []
            };
            await CustomInstallerProjectStore.SaveAsync(ProjectPath, project);
            _status.SetSuccess($"Created project: {ProjectPath}");
            _openWorkspace(project, ProjectPath, OutputDirectory);
        }
        catch (Exception ex)
        {
            _status.SetError(ex.Message);
            SetCurrentStep(3);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreateProject() => IsCreateStep && !IsBusy && IsStepValid(3);

    private void SetCurrentStep(int index)
    {
        CurrentStepIndex = Math.Clamp(index, 0, Steps.Count - 1);
        CurrentStepTitle = Steps[CurrentStepIndex].Title;
        CurrentStepSubtitle = Steps[CurrentStepIndex].Subtitle;
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsCurrent = i == CurrentStepIndex;
            Steps[i].IsComplete = i < CurrentStepIndex && IsStepValid(i);
        }
        IsProjectStep = CurrentStepIndex == 0;
        IsMetadataStep = CurrentStepIndex == 1;
        IsOutputStep = CurrentStepIndex == 2;
        IsCreateStep = CurrentStepIndex == 3;
        NotifyStepState();
    }

    private void NotifyStepState()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        NextStepCommand.NotifyCanExecuteChanged();
        PreviousStepCommand.NotifyCanExecuteChanged();
        CreateProjectCommand.NotifyCanExecuteChanged();
    }

    private bool AllPriorStepsValid(int index)
    {
        for (var i = 0; i < index; i++)
            if (!IsStepValid(i)) return false;
        return true;
    }

    private bool IsStepValid(int index) => index switch
    {
        0 => !string.IsNullOrWhiteSpace(ProjectPath),
        1 => !string.IsNullOrWhiteSpace(PackageName) && !string.IsNullOrWhiteSpace(Version) && !string.IsNullOrWhiteSpace(Maintainer) && !string.IsNullOrWhiteSpace(Description),
        2 => !string.IsNullOrWhiteSpace(OutputDirectory),
        3 => AllPriorStepsValid(3) && !IsBusy,
        _ => false
    };

    partial void OnCurrentStepIndexChanged(int value) => NotifyStepState();
    partial void OnProjectPathChanged(string value) => NotifyStepState();
    partial void OnPackageNameChanged(string value) => NotifyStepState();
    partial void OnVersionChanged(string value) => NotifyStepState();
    partial void OnMaintainerChanged(string value) => NotifyStepState();
    partial void OnDescriptionChanged(string value) => NotifyStepState();
    partial void OnOutputDirectoryChanged(string value) => NotifyStepState();
    partial void OnIsBusyChanged(bool value) => NotifyStepState();
}
