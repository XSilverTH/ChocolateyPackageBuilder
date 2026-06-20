using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;
using ChocolateyPackageBuilder.Gui.Models;
using ChocolateyPackageBuilder.Gui.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsStore _appSettingsStore;
    private readonly AppStatusViewModel _status;

    public SettingsViewModel(IAppSettingsStore appSettingsStore, AppStatusViewModel status)
    {
        _status = status;
        _appSettingsStore = appSettingsStore;

        var settings = _appSettingsStore.LoadOrDefault(out var warning);
        if (warning is not null) _status.SetError(warning);
        ThemeMode = settings.ThemeMode;
        VerboseStatus = settings.VerboseStatus;
        RememberWorkspaceLayout = settings.RememberWorkspaceLayout;
        ProjectSidebarExpanded = settings.ProjectSidebarExpanded;
        ScriptPreviewExpanded = settings.ScriptPreviewExpanded;
    }

    [ObservableProperty] public partial bool ProjectSidebarExpanded { get; set; }

    [ObservableProperty] public partial bool RememberWorkspaceLayout { get; set; }

    [ObservableProperty] public partial bool ScriptPreviewExpanded { get; set; }

    [ObservableProperty] public partial AppThemeMode ThemeMode { get; set; }

    [ObservableProperty] public partial bool VerboseStatus { get; set; }

    public IReadOnlyList<AppThemeMode> ThemeModes { get; } = Enum.GetValues<AppThemeMode>();

    public void ApplyTheme()
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = ThemeMode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private AppSettings ToSettings()
    {
        return new AppSettings
        {
            ThemeMode = ThemeMode,
            VerboseStatus = VerboseStatus,
            RememberWorkspaceLayout = RememberWorkspaceLayout,
            ProjectSidebarExpanded = ProjectSidebarExpanded,
            ScriptPreviewExpanded = ScriptPreviewExpanded
        };
    }

    partial void OnThemeModeChanged(AppThemeMode value)
    {
        Save();
        ApplyTheme();
    }

    partial void OnVerboseStatusChanged(bool value)
    {
        Save();
    }

    partial void OnRememberWorkspaceLayoutChanged(bool value)
    {
        Save();
    }

    partial void OnProjectSidebarExpandedChanged(bool value)
    {
        Save();
    }

    partial void OnScriptPreviewExpandedChanged(bool value)
    {
        Save();
    }

    private void Save()
    {
        try
        {
            _appSettingsStore.Save(ToSettings());
        }
        catch (Exception ex)
        {
            _status.SetError("Settings could not be saved.", ex.Message);
        }
    }
}