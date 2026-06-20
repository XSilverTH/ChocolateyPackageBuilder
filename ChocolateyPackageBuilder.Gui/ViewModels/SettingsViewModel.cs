using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;
using ChocolateyPackageBuilder.Gui.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly AppStatusViewModel _status;

    [ObservableProperty] private bool projectSidebarExpanded;
    [ObservableProperty] private bool rememberWorkspaceLayout = true;
    [ObservableProperty] private bool scriptPreviewExpanded = true;
    [ObservableProperty] private AppThemeMode themeMode;
    [ObservableProperty] private bool verboseStatus;

    public SettingsViewModel(AppSettings settings, AppStatusViewModel status)
    {
        _status = status;
        themeMode = settings.ThemeMode;
        verboseStatus = settings.VerboseStatus;
        rememberWorkspaceLayout = settings.RememberWorkspaceLayout;
        projectSidebarExpanded = settings.ProjectSidebarExpanded;
        scriptPreviewExpanded = settings.ScriptPreviewExpanded;
    }

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

    public AppSettings ToSettings() => new()
    {
        ThemeMode = ThemeMode,
        VerboseStatus = VerboseStatus,
        RememberWorkspaceLayout = RememberWorkspaceLayout,
        ProjectSidebarExpanded = ProjectSidebarExpanded,
        ScriptPreviewExpanded = ScriptPreviewExpanded
    };

    partial void OnThemeModeChanged(AppThemeMode value)
    {
        Save();
        ApplyTheme();
    }

    partial void OnVerboseStatusChanged(bool value) => Save();
    partial void OnRememberWorkspaceLayoutChanged(bool value) => Save();
    partial void OnProjectSidebarExpandedChanged(bool value) => Save();
    partial void OnScriptPreviewExpandedChanged(bool value) => Save();

    private void Save()
    {
        try
        {
            AppSettingsStore.Save(ToSettings());
        }
        catch (Exception ex)
        {
            _status.SetError("Settings could not be saved.", ex.Message);
        }
    }
}
