using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChocolateyPackageBuilder.Core;
using ChocolateyPackageBuilder.Gui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateyPackageBuilder.Gui.Features.CustomInstallerProject;

public partial class CustomInstallerProjectViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BuildProjectCommand))] private bool isBusy;
    [ObservableProperty] private string maintainer = PackageUtility.DefaultMaintainer();
    [ObservableProperty] private string outputDirectory = Environment.CurrentDirectory;
    [ObservableProperty] private string packageName = string.Empty;
    [ObservableProperty] private string projectPath = string.Empty;
    [ObservableProperty] private string scriptPreview = "Add project files and actions to preview the generated Chocolatey install script.";
    [ObservableProperty] private string statusMessage = "Ready.";
    [ObservableProperty] private string version = "1.0.0";

    public CustomInstallerProjectViewModel(IFileDialogService fileDialogService)
    {
        _fileDialogService = fileDialogService;
    }

    public ObservableCollection<ProjectFileItemViewModel> ProjectFiles { get; } = [];
    public ObservableCollection<ActionItemViewModel> Actions { get; } = [];

    [RelayCommand]
    private void NewProject()
    {
        ProjectPath = string.Empty;
        PackageName = string.Empty;
        Version = "1.0.0";
        Maintainer = PackageUtility.DefaultMaintainer();
        Description = string.Empty;
        OutputDirectory = Environment.CurrentDirectory;
        ProjectFiles.Clear();
        Actions.Clear();
        StatusMessage = "New project.";
        RefreshScriptPreview();
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var path = await _fileDialogService.PickProjectFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var project = await CustomInstallerProjectStore.LoadAsync(path);
            LoadProject(project);
            ProjectPath = path;
            StatusMessage = $"Opened project: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

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
            StatusMessage = "Save the project before adding files.";
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
            ProjectFiles.Add(new ProjectFileItemViewModel
            {
                Id = CreateUniqueFileId(Path.GetFileNameWithoutExtension(fileName)),
                SourcePath = relativePath,
                PackagePath = relativePath
            });
            StatusMessage = $"Added file: {fileName}";
            RefreshScriptPreview();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void RemoveProjectFile(ProjectFileItemViewModel? file)
    {
        if (file is null) return;
        ProjectFiles.Remove(file);
        RefreshScriptPreview();
    }

    [RelayCommand]
    private void AddCopyAction()
    {
        var firstFileId = ProjectFiles.FirstOrDefault()?.Id ?? string.Empty;
        var action = new ActionItemViewModel { Kind = InstallActionKind.CopyFile, SourceKind = ActionPathKind.PackageFile, SourceValue = firstFileId, DestinationKind = ActionPathKind.Literal, Overwrite = true };
        TrackAction(action);
        Actions.Add(action);
        RefreshScriptPreview();
    }

    [RelayCommand]
    private void AddRunAction()
    {
        var firstFileId = ProjectFiles.FirstOrDefault()?.Id ?? string.Empty;
        var action = new ActionItemViewModel { Kind = InstallActionKind.RunFile, SourceKind = ActionPathKind.PackageFile, SourceValue = firstFileId, WaitForExit = true, ValidExitCodesText = "0" };
        TrackAction(action);
        Actions.Add(action);
        RefreshScriptPreview();
    }

    [RelayCommand]
    private void RemoveAction(ActionItemViewModel? action)
    {
        if (action is null) return;
        Actions.Remove(action);
        RefreshScriptPreview();
    }

    [RelayCommand]
    private void MoveActionUp(ActionItemViewModel? action)
    {
        if (action is null) return;
        var index = Actions.IndexOf(action);
        if (index <= 0) return;
        Actions.Move(index, index - 1);
        RefreshScriptPreview();
    }

    [RelayCommand]
    private void MoveActionDown(ActionItemViewModel? action)
    {
        if (action is null) return;
        var index = Actions.IndexOf(action);
        if (index < 0 || index >= Actions.Count - 1) return;
        Actions.Move(index, index + 1);
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
        StatusMessage = "Building project package...";
        try
        {
            await SaveProjectToAsync(ProjectPath);
            var result = await PackageGenerator.PackCustomProjectAsync(new CustomProjectPackRequest(ProjectPath, OutputDirectory));
            StatusMessage = $"Built project package: {result.OutputPath}";
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

    [RelayCommand]
    private async Task BrowseOutputDirectoryAsync()
    {
        var path = await _fileDialogService.PickOutputDirectoryAsync();
        if (!string.IsNullOrWhiteSpace(path)) OutputDirectory = path;
    }

    public ActionPathKind[] ActionPathKinds { get; } = [ActionPathKind.PackageFile, ActionPathKind.Literal];

    private bool CanBuildProject()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(OutputDirectory);
    }

    private async Task SaveProjectToAsync(string path)
    {
        var project = CreateProject();
        await CustomInstallerProjectStore.SaveAsync(path, project);
        ProjectPath = path;
        StatusMessage = $"Saved project: {path}";
        RefreshScriptPreview();
    }

    private global::ChocolateyPackageBuilder.Core.CustomInstallerProject CreateProject()
    {
        return new global::ChocolateyPackageBuilder.Core.CustomInstallerProject
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

    private void LoadProject(global::ChocolateyPackageBuilder.Core.CustomInstallerProject project)
    {
        PackageName = project.Package.Name;
        Version = project.Package.Version;
        Maintainer = project.Package.Maintainer;
        Description = project.Package.Description;
        ProjectFiles.Clear();
        foreach (var file in project.Files)
            ProjectFiles.Add(new ProjectFileItemViewModel { Id = file.Id, SourcePath = file.SourcePath, PackagePath = file.PackagePath });

        Actions.Clear();
        foreach (var action in project.Actions)
        {
            var item = new ActionItemViewModel { Kind = action.Kind, Arguments = action.Arguments, WaitForExit = action.WaitForExit, ValidExitCodesText = string.Join(",", action.ValidExitCodes), Overwrite = action.Overwrite };
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
        RefreshScriptPreview();
    }

    private void TrackAction(ActionItemViewModel action)
    {
        action.PropertyChanged += (_, _) => RefreshScriptPreview();
    }

    partial void OnPackageNameChanged(string value) => RefreshScriptPreview();
    partial void OnVersionChanged(string value) => RefreshScriptPreview();
    partial void OnMaintainerChanged(string value) => RefreshScriptPreview();
    partial void OnDescriptionChanged(string value) => RefreshScriptPreview();

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
    [ObservableProperty] private string sourcePath = string.Empty;
    [ObservableProperty] private string packagePath = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Id) ? SourcePath : Id;

    partial void OnIdChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnSourcePathChanged(string value) => OnPropertyChanged(nameof(DisplayName));
}

public partial class ActionItemViewModel : ObservableObject
{
    [ObservableProperty] private string arguments = string.Empty;
    [ObservableProperty] private ActionPathKind destinationKind = ActionPathKind.Literal;
    [ObservableProperty] private string destinationValue = string.Empty;
    [ObservableProperty] private InstallActionKind kind;
    [ObservableProperty] private bool overwrite = true;
    [ObservableProperty] private ActionPathKind sourceKind = ActionPathKind.PackageFile;
    [ObservableProperty] private string sourceValue = string.Empty;
    [ObservableProperty] private string validExitCodesText = "0";
    [ObservableProperty] private bool waitForExit = true;

    public string Summary => Kind == InstallActionKind.CopyFile
        ? $"Copy {SourceValue} to {DestinationValue}"
        : $"Run {SourceValue} {Arguments}".Trim();

    partial void OnArgumentsChanged(string value) => OnPropertyChanged(nameof(Summary));
    partial void OnDestinationValueChanged(string value) => OnPropertyChanged(nameof(Summary));
    partial void OnKindChanged(InstallActionKind value) => OnPropertyChanged(nameof(Summary));
    partial void OnSourceValueChanged(string value) => OnPropertyChanged(nameof(Summary));
}
