using System.Text;

namespace ChocolateyPackageBuilder.Core;

public enum InstallerType
{
    Msi,
    InnoSetup,
    Nsis,
    Unknown
}

public static class InstallerDetector
{
    public static InstallerType Detect(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Installer not found: {filePath}", filePath);

        const int maxBytesToRead = 2 * 1024 * 1024;
        var buffer = new byte[maxBytesToRead];
        int bytesRead;

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            bytesRead = fs.Read(buffer, 0, maxBytesToRead);
        }

        if (bytesRead >= 8 &&
            buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0 &&
            buffer[4] == 0xA1 && buffer[5] == 0xB1 && buffer[6] == 0x1A && buffer[7] == 0xE1)
            return InstallerType.Msi;

        var fileContent = Encoding.ASCII.GetString(buffer, 0, bytesRead);

        if (fileContent.Contains("Inno Setup", StringComparison.OrdinalIgnoreCase)) return InstallerType.InnoSetup;

        return fileContent.Contains("Nullsoft", StringComparison.OrdinalIgnoreCase)
            ? InstallerType.Nsis
            : InstallerType.Unknown;
    }
}