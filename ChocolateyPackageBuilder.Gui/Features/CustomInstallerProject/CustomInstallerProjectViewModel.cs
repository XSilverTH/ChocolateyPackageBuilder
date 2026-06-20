using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChocolateyPackageBuilder.Core;
using ChocolateyPackageBuilder.Gui.Services;
using ChocolateyPackageBuilder.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateyPackageBuilder.Gui.Features.CustomInstallerProject;

public partial class CustomInstallerProjectViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly SettingsViewModel _settings;
    private readonly AppStatusViewModel _status;
    private bool _loading;
    private readonly Action _closeAction;

    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildProjectCommand))] private bool isBusy;
    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private bool isMetadataEditorOpen;
    [ObservableProperty] private bool isScriptPreviewExpanded;
    [ObservableProperty] private bool isSidebarExpanded;
    [ObservableProperty] private string maintainer = PackageUtility.DefaultMaintainer();
    [ObservableProperty] private string outputDirectory = Environment.CurrentDirectory;
    [ObservableProperty] private string packageName = string.Empty;
    [ObservableProperty] private string projectPath = string.Empty;
    [ObservableProperty] private string scriptPreview = "Add project files and actions to preview the generated Chocolatey install script.";
    [ObservableProperty] private string version = "1.0.0";

    public CustomInstallerProjectViewModel(
        IFileDialogService fileDialogService,
        AppStatusViewModel status,
        SettingsViewModel settings,
        ChocolateyPackageBuilder.Core.CustomInstallerProject project,
        string projectPath,
        string outputDirectory,
        Action closeAction)
    {
        _closeAction = closeAction;
        _fileDialogService = fileDialogService;
        _status = status;
        _settings = settings;
        IsSidebarExpanded = settings.RememberWorkspaceLayout ? settings.ProjectSidebarExpanded : true;
        IsScriptPreviewExpanded = settings.RememberWorkspaceLayout ? settings.ScriptPreviewExpanded : true;
        LoadProject(project);
        ProjectPath = projectPath;
        OutputDirectory = outputDirectory;
        IsDirty = false;
        _status.SetSuccess($"Opened project: {projectPath}");
    }

    [RelayCommand]
    private void CloseProject() => _closeAction();

    public ObservableCollection<ProjectFileItemViewModel> ProjectFiles { get; } = [];
    public ObservableCollection<ActionItemViewModel> Actions { get; } = [];
    public ActionPathKind[] ActionPathKinds { get; } = [ActionPathKind.PackageFile, ActionPathKind.Literal];
    public IReadOnlyList<ComponentDefinitionViewModel> Components { get; } =
    [
        new ComponentDefinitionViewModel(InstallActionKind.CopyFile, "Copy file", "Copy a bundled project file to an install path."),
        new ComponentDefinitionViewModel(InstallActionKind.RunFile, "Run file", "Launch a bundled or literal file with arguments.")
    ];
    public double SidebarWidth => IsSidebarExpanded ? 280 : 56;
    public double ScriptPreviewWidth => IsScriptPreviewExpanded ? 420 : 44;
    public bool IsSidebarCollapsed => !IsSidebarExpanded;
    public bool IsScriptPreviewCollapsed => !IsScriptPreviewExpanded;


    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            await SaveProjectAsAsync();
            return;
        }

        await SaveProjectToAsync(ProjectPath);
    }

    [RelayCommand]
    private async Task SaveProjectAsAsync()
    {
        var path = await _fileDialogService.SaveProjectFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        if (!Path.GetExtension(path).Equals(CustomInstallerProjectStore.FileExtension, StringComparison.OrdinalIgnoreCase))
            path += CustomInstallerProjectStore.FileExtension;

        await SaveProjectToAsync(path);
    }

    [RelayCommand]
    private async Task AddProjectFileAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            _status.SetError("Save the project before adding files.");
            return;
        }

        var selectedPath = await _fileDialogService.PickAnyFileAsync();
        if (string.IsNullOrWhiteSpace(selectedPath)) return;

        try
        {
            var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(ProjectPath)) ?? Environment.CurrentDirectory;
            var filesDirectory = Path.Combine(projectDirectory, "files");
            Directory.CreateDirectory(filesDirectory);

            var fileName = CreateUniqueFileName(filesDirectory, Path.GetFileName(selectedPath));
            var destinationPath = Path.Combine(filesDirectory, fileName);
            File.Copy(selectedPath, destinationPath, overwrite: false);

            var relativePath = $"files/{fileName}";
            var file = new ProjectFileItemViewModel
            {
                Id = CreateUniqueFileId(Path.GetFileNameWithoutExtension(fileName)),
                SourcePath = relativePath,
                PackagePath = relativePath
            };
            TrackProjectFile(file);
            ProjectFiles.Add(file);
            MarkDirty();
            _status.SetSuccess($"Added file: {fileName}", _settings.VerboseStatus ? relativePath : string.Empty);
            RefreshScriptPreview();
        }
        catch (Exception ex)
        {
            _status.SetError(ex.Message);
        }
    }

    [RelayCommand]
    private void RemoveProjectFile(ProjectFileItemViewModel? file)
    {
        if (file is null) return;
        ProjectFiles.Remove(file);
        MarkDirty();
        RefreshScriptPreview();
    }

    [RelayCommand]
    private void AddComponent(ComponentDefinitionViewModel? component)
    {
        if (component is null) return;
        var action = CreateDefaultAction(component.Kind);
        action.IsExpanded = true;
        TrackAction(action);
        Actions.Add(action);
        MarkDirty();
        RefreshScriptPreview();
    }

    [RelayCommand]
    private void RemoveAction(ActionItemViewModel? action)
    {
        if (action is null) return;
        Actions.Remove(action);
        MarkDirty();
        RefreshScriptPreview();
    }

    [RelayCommand(CanExecute = nameof(CanBuildProject))]
    private async Task BuildProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            await SaveProjectAsAsync();
            if (string.IsNullOrWhiteSpace(ProjectPath)) return;
        }

        IsBusy = true;
        _status.SetBusy("Building project package...", _settings.VerboseStatus ? ProjectPath : string.Empty);
        try
        {
            await SaveProjectToAsync(ProjectPath, reportStatus: false);
            var result = await PackageGenerator.PackCustomProjectAsync(new CustomProjectPackRequest(ProjectPath, OutputDirectory));
            _status.SetSuccess($"Built project package: {result.OutputPath}");
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
    private async Task BrowseOutputDirectoryAsync()
    {
        var path = await _fileDialogService.PickOutputDirectoryAsync();
        if (!string.IsNullOrWhiteSpace(path)) OutputDirectory = path;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        if (_settings.RememberWorkspaceLayout) _settings.ProjectSidebarExpanded = IsSidebarExpanded;
    }

    [RelayCommand]
    private void ToggleScriptPreview()
    {
        IsScriptPreviewExpanded = !IsScriptPreviewExpanded;
        if (_settings.RememberWorkspaceLayout) _settings.ScriptPreviewExpanded = IsScriptPreviewExpanded;
    }
    partial void OnIsSidebarExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(SidebarWidth));
        OnPropertyChanged(nameof(IsSidebarCollapsed));
    }

    partial void OnIsScriptPreviewExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ScriptPreviewWidth));
        OnPropertyChanged(nameof(IsScriptPreviewCollapsed));
    }


    [RelayCommand]
    private void OpenMetadataEditor() => IsMetadataEditorOpen = true;

    [RelayCommand]
    private void CloseMetadataEditor() => IsMetadataEditorOpen = false;

    public void MoveActionBefore(ActionItemViewModel? dragged, ActionItemViewModel? target)
    {
        if (dragged is null || target is null || ReferenceEquals(dragged, target)) return;
        var oldIndex = Actions.IndexOf(dragged);
        var targetIndex = Actions.IndexOf(target);
        if (oldIndex < 0 || targetIndex < 0 || oldIndex == targetIndex) return;
        if (oldIndex < targetIndex) targetIndex--;
        Actions.Move(oldIndex, targetIndex);
        MarkDirty();
        RefreshScriptPreview();
        if (_settings.VerboseStatus) _status.SetSuccess("Action moved.", $"Moved action to position {targetIndex + 1}.");
    }

    public void MoveActionToEnd(ActionItemViewModel? dragged)
    {
        if (dragged is null) return;
        var oldIndex = Actions.IndexOf(dragged);
        if (oldIndex < 0 || oldIndex == Actions.Count - 1) return;
        Actions.Move(oldIndex, Actions.Count - 1);
        MarkDirty();
        RefreshScriptPreview();
        if (_settings.VerboseStatus) _status.SetSuccess("Action moved.", $"Moved action to position {Actions.Count}.");
    }

    private bool CanBuildProject() => !IsBusy && !string.IsNullOrWhiteSpace(OutputDirectory);

    private async Task SaveProjectToAsync(string path, bool reportStatus = true)
    {
        await CustomInstallerProjectStore.SaveAsync(path, CreateProject());
        ProjectPath = path;
        IsDirty = false;
        RefreshScriptPreview();
        if (reportStatus) _status.SetSuccess($"Saved project: {path}");
    }

    private ChocolateyPackageBuilder.Core.CustomInstallerProject CreateProject()
    {
        return new ChocolateyPackageBuilder.Core.CustomInstallerProject
        {
            Package = new PackageMetadata { Name = PackageName.Trim(), Version = Version.Trim(), Maintainer = Maintainer.Trim(), Description = Description.Trim() },
            Files = ProjectFiles.Select(file => new ProjectFile { Id = file.Id.Trim(), SourcePath = file.SourcePath.Trim(), PackagePath = file.PackagePath.Trim() }).ToList(),
            Actions = Actions.Select(CreateAction).ToList()
        };
    }

    private static InstallAction CreateAction(ActionItemViewModel item)
    {
        var action = new InstallAction { Kind = item.Kind, Arguments = item.Arguments.Trim(), WaitForExit = item.WaitForExit, ValidExitCodes = ParseExitCodes(item.ValidExitCodesText), Overwrite = item.Overwrite };
        if (item.Kind == InstallActionKind.CopyFile)
        {
            action.Source = new ActionPath { Kind = item.SourceKind, Value = item.SourceValue.Trim() };
            action.Destination = new ActionPath { Kind = item.DestinationKind, Value = item.DestinationValue.Trim() };
        }
        else
        {
            action.File = new ActionPath { Kind = item.SourceKind, Value = item.SourceValue.Trim() };
        }
        return action;
    }

    private void LoadProject(ChocolateyPackageBuilder.Core.CustomInstallerProject project)
    {
        _loading = true;
        PackageName = project.Package.Name;
        Version = project.Package.Version;
        Maintainer = project.Package.Maintainer;
        Description = project.Package.Description;
        ProjectFiles.Clear();
        foreach (var file in project.Files)
        {
            var item = new ProjectFileItemViewModel { Id = file.Id, SourcePath = file.SourcePath, PackagePath = file.PackagePath };
            TrackProjectFile(item);
            ProjectFiles.Add(item);
        }

        Actions.Clear();
        foreach (var action in project.Actions)
        {
            var item = new ActionItemViewModel { Kind = action.Kind, Arguments = action.Arguments, WaitForExit = action.WaitForExit, ValidExitCodesText = string.Join(",", action.ValidExitCodes), Overwrite = action.Overwrite, IsExpanded = false };
            if (action.Kind == InstallActionKind.CopyFile)
            {
                item.SourceKind = action.Source.Kind;
                item.SourceValue = action.Source.Value;
                item.DestinationKind = action.Destination.Kind;
                item.DestinationValue = action.Destination.Value;
            }
            else
            {
                item.SourceKind = action.File.Kind;
                item.SourceValue = action.File.Value;
            }
            TrackAction(item);
            Actions.Add(item);
        }
        _loading = false;
        RefreshScriptPreview();
    }

    private void TrackProjectFile(ProjectFileItemViewModel file)
    {
        file.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(ProjectFileItemViewModel.Id) or nameof(ProjectFileItemViewModel.PackagePath)) MarkDirty();
            RefreshScriptPreview();
        };
    }

    private void TrackAction(ActionItemViewModel action)
    {
        action.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not nameof(ActionItemViewModel.IsExpanded)) MarkDirty();
            RefreshScriptPreview();
        };
    }

    private ActionItemViewModel CreateDefaultAction(InstallActionKind kind)
    {
        var firstFileId = ProjectFiles.FirstOrDefault()?.Id ?? string.Empty;
        return kind == InstallActionKind.CopyFile
            ? new ActionItemViewModel { Kind = InstallActionKind.CopyFile, SourceKind = ActionPathKind.PackageFile, SourceValue = firstFileId, DestinationKind = ActionPathKind.Literal, Overwrite = true }
            : new ActionItemViewModel { Kind = InstallActionKind.RunFile, SourceKind = ActionPathKind.PackageFile, SourceValue = firstFileId, WaitForExit = true, ValidExitCodesText = "0" };
    }

    partial void OnPackageNameChanged(string value)
    {
        MarkDirty();
        RefreshScriptPreview();
    }

    partial void OnVersionChanged(string value)
    {
        MarkDirty();
        RefreshScriptPreview();
    }

    partial void OnMaintainerChanged(string value)
    {
        MarkDirty();
        RefreshScriptPreview();
    }

    partial void OnDescriptionChanged(string value)
    {
        MarkDirty();
        RefreshScriptPreview();
    }

    private void MarkDirty()
    {
        if (!_loading) IsDirty = true;
    }

    private void RefreshScriptPreview()
    {
        try
        {
            ScriptPreview = CustomInstallerScriptGenerator.Generate(CreateProject());
        }
        catch (Exception ex)
        {
            ScriptPreview = ex.Message;
        }
    }

    private string CreateUniqueFileId(string fileName)
    {
        var baseId = PackageUtility.CreatePackageSlug(fileName);
        var id = baseId;
        var index = 2;
        while (ProjectFiles.Any(file => file.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            id = $"{baseId}-{index++}";
        return id;
    }

    private static string CreateUniqueFileName(string directory, string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidate = fileName;
        var index = 2;
        while (File.Exists(Path.Combine(directory, candidate)))
            candidate = string.Create(CultureInfo.InvariantCulture, $"{name}-{index++}{extension}");
        return candidate;
    }

    private static List<int> ParseExitCodes(string value)
    {
        var result = value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) ? code : 0)
            .ToList();
        return result.Count == 0 ? [0] : result;
    }
}

public partial class ProjectFileItemViewModel : ObservableObject
{
    [ObservableProperty] private string id = string.Empty;
    [ObservableProperty] private string packagePath = string.Empty;
    [ObservableProperty] private string sourcePath = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Id) ? SourcePath : Id;

    partial void OnIdChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnSourcePathChanged(string value) => OnPropertyChanged(nameof(DisplayName));
}

public partial class ActionItemViewModel : ObservableObject
{
    [ObservableProperty] private string arguments = string.Empty;
    [ObservableProperty] private ActionPathKind destinationKind = ActionPathKind.Literal;
    [ObservableProperty] private string destinationValue = string.Empty;
    [ObservableProperty] private bool isExpanded = true;
    [ObservableProperty] private InstallActionKind kind;
    [ObservableProperty] private bool overwrite = true;
    [ObservableProperty] private ActionPathKind sourceKind = ActionPathKind.PackageFile;
    [ObservableProperty] private string sourceValue = string.Empty;
    [ObservableProperty] private string validExitCodesText = "0";
    [ObservableProperty] private bool waitForExit = true;

    public bool IsCopyFile => Kind == InstallActionKind.CopyFile;
    public bool IsRunFile => Kind == InstallActionKind.RunFile;
    public string Summary => Kind == InstallActionKind.CopyFile ? $"Copy {SourceValue} to {DestinationValue}" : $"Run {SourceValue} {Arguments}".Trim();

    partial void OnArgumentsChanged(string value) => OnPropertyChanged(nameof(Summary));
    partial void OnDestinationValueChanged(string value) => OnPropertyChanged(nameof(Summary));
    partial void OnKindChanged(InstallActionKind value)
    {
        OnPropertyChanged(nameof(IsCopyFile));
        OnPropertyChanged(nameof(IsRunFile));
        OnPropertyChanged(nameof(Summary));
    }
    partial void OnSourceValueChanged(string value) => OnPropertyChanged(nameof(Summary));
}
