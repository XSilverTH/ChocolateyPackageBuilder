using System.Text;

namespace ChocolateyPackageBuilder.Services;

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
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Installer not found: {filePath}");
        }

        // Read up to 2MB
        const int maxBytesToRead = 2 * 1024 * 1024;
        var buffer = new byte[maxBytesToRead];
        int bytesRead;

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            bytesRead = fs.Read(buffer, 0, maxBytesToRead);
        }

        // 1. MSI check
        // OLE Compound File: D0 CF 11 E0 A1 B1 1A E1
        if (bytesRead >= 8)
        {
            if (buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0 &&
                buffer[4] == 0xA1 && buffer[5] == 0xB1 && buffer[6] == 0x1A && buffer[7] == 0xE1)
            {
                return InstallerType.Msi;
            }
        }

        // Convert read buffer to ASCII string to search for texts
        // Using ASCII because we just look for basic ASCII characters, even if it's UTF-16, 
        // the string representation is distinct enough.
        var fileContent = Encoding.ASCII.GetString(buffer, 0, bytesRead);

        // 2. Inno Setup check
        if (fileContent.Contains("Inno Setup", StringComparison.OrdinalIgnoreCase))
        {
            return InstallerType.InnoSetup;
        }

        // 3. NSIS check
        return fileContent.Contains("Nullsoft", StringComparison.OrdinalIgnoreCase) ? InstallerType.Nsis : InstallerType.Unknown;
    }
}