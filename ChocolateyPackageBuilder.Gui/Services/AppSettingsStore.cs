using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChocolateyPackageBuilder.Gui.Services;

public static class AppSettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppSettings LoadOrDefault(out string? warning)
    {
        warning = null;
        var path = GetSettingsPath();
        if (!File.Exists(path)) return new AppSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            warning = $"Settings could not be loaded; defaults were applied: {ex.Message}";
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var path = GetSettingsPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, Options));
    }

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(appData)
            ? Path.Combine(Environment.CurrentDirectory, ".chocolatey-package-builder-settings.json")
            : Path.Combine(appData, "ChocolateyPackageBuilder", "settings.json");
    }
}
