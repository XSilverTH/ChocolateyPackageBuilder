using System;
using System.Threading.Tasks;
using ChocolateyPackageBuilder.Core.Interfaces;
using ChocolateyPackageBuilder.Core.Models;
using ChocolateyPackageBuilder.Gui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICustomInstallerProjectStore _customInstallerProjectStore;
    private readonly ICustomInstallerScriptGenerator _customInstallerScriptGenerator;
    private readonly IFileDialogService _fileDialogService;
    private readonly IPackageGenerator _packageGenerator;

    [ObservableProperty] private ViewModelBase currentPage;
    [ObservableProperty] private CustomInstallerProjectViewModel? currentProjectWorkspace;
    [ObservableProperty] private bool isSettingsOpen;

    public MainWindowViewModel(
        IFileDialogService fileDialogService,
        AppStatusViewModel status,
        SettingsViewModel settings,
        PackageBuilderViewModel packageBuilderViewModel,
        ICustomInstallerProjectStore customInstallerProjectStore,
        IPackageGenerator packageGenerator,
        ICustomInstallerScriptGenerator customInstallerScriptGenerator)
    {
        _fileDialogService = fileDialogService;
        Status = status;
        Settings = settings;
        Settings.ApplyTheme();
        QuickInstallerPage = packageBuilderViewModel;
        _customInstallerProjectStore = customInstallerProjectStore;
        _packageGenerator = packageGenerator;
        _customInstallerScriptGenerator = customInstallerScriptGenerator;

        CustomProjectHubPage = new CustomProjectHubViewModel(this);
        currentPage = QuickInstallerPage;
    }

    public PackageBuilderViewModel QuickInstallerPage { get; }
    public CustomProjectHubViewModel CustomProjectHubPage { get; }
    public AppStatusViewModel Status { get; }
    public SettingsViewModel Settings { get; }
    public bool HasOpenCustomProject => CurrentProjectWorkspace is not null;
    public bool IsQuickInstallerActive => CurrentPage == QuickInstallerPage;

    public bool IsCustomProjectActive => CurrentPage is CustomProjectHubViewModel or CustomProjectSetupViewModel
        or CustomInstallerProjectViewModel;

    [RelayCommand]
    private void ShowQuickInstaller()
    {
        CurrentPage = QuickInstallerPage;
        Status.SetReady("Quick installer wizard");
    }

    [RelayCommand]
    private void ShowCustomProject()
    {
        if (CurrentProjectWorkspace is not null)
        {
            CurrentPage = CurrentProjectWorkspace;
            Status.SetReady("Custom project workspace");
        }
        else
        {
            CurrentPage = CustomProjectHubPage;
            Status.SetReady("Custom project");
        }
    }

    [RelayCommand]
    private void NewCustomProject()
    {
        if (CurrentProjectWorkspace?.IsDirty == true)
        {
            Status.SetError("Save the current project before creating or opening another project.");
            return;
        }

        CurrentPage = new CustomProjectSetupViewModel(_fileDialogService, Status, Settings, OpenCustomProjectWorkspace,
            _customInstallerProjectStore);
        Status.SetReady("New custom project wizard");
    }

    [RelayCommand]
    private async Task OpenCustomProjectAsync()
    {
        if (CurrentProjectWorkspace?.IsDirty == true)
        {
            Status.SetError("Save the current project before creating or opening another project.");
            return;
        }

        var path = await _fileDialogService.PickProjectFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var project = await _customInstallerProjectStore.LoadAsync(path);
            OpenCustomProjectWorkspace(project, path, Environment.CurrentDirectory);
        }
        catch (Exception ex)
        {
            Status.SetError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (CurrentProjectWorkspace is not null) CurrentProjectWorkspace.IsMetadataEditorOpen = false;
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    private void OpenCustomProjectWorkspace(CustomInstallerProject project, string projectPath, string outputDirectory)
    {
        CurrentProjectWorkspace = new CustomInstallerProjectViewModel(_fileDialogService, Status, Settings, project,
            projectPath, outputDirectory, CloseCustomProjectWorkspace, _customInstallerProjectStore, _packageGenerator,
            _customInstallerScriptGenerator);
        CurrentPage = CurrentProjectWorkspace;
        OnPropertyChanged(nameof(HasOpenCustomProject));
        OnPropertyChanged(nameof(IsCustomProjectActive));
    }

    private void CloseCustomProjectWorkspace()
    {
        if (CurrentProjectWorkspace?.IsDirty == true)
        {
            Status.SetError("Save the current project before closing.");
            return;
        }

        CurrentProjectWorkspace = null;
        ShowCustomProjectCommand.Execute(null);
    }

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsQuickInstallerActive));
        OnPropertyChanged(nameof(IsCustomProjectActive));
    }

    partial void OnCurrentProjectWorkspaceChanged(CustomInstallerProjectViewModel? value)
    {
        OnPropertyChanged(nameof(HasOpenCustomProject));
    }

    partial void OnIsSettingsOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsQuickInstallerActive));
        OnPropertyChanged(nameof(IsCustomProjectActive));
    }
}