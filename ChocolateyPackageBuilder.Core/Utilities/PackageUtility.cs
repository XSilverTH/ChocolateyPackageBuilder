using System.Text.RegularExpressions;

namespace ChocolateyPackageBuilder.Core.Utilities;

public static partial class PackageUtility
{
    public static string CreatePackageSlug(string value)
    {
        var slug = SlugUnsafeCharacters().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "package" : slug;
    }

    public static string DefaultMaintainer()
    {
        return string.IsNullOrWhiteSpace(Environment.UserName) ? "Unknown" : Environment.UserName;
    }

    [GeneratedRegex("[^a-z0-9.-]+")]
    private static partial Regex SlugUnsafeCharacters();
}