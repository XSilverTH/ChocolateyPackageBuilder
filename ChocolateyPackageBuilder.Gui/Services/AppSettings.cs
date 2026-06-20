namespace ChocolateyPackageBuilder.Gui.Services;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed class AppSettings
{
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public bool VerboseStatus { get; set; }
    public bool RememberWorkspaceLayout { get; set; } = true;
    public bool ProjectSidebarExpanded { get; set; } = true;
    public bool ScriptPreviewExpanded { get; set; } = true;
}
