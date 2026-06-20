using System.Buffers;

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

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            // First read 8 bytes to quickly check for MSI signature
            Span<byte> header = stackalloc byte[8];
            int headerBytesRead = fs.Read(header);

            if (headerBytesRead >= 8 &&
                header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0 &&
                header[4] == 0xA1 && header[5] == 0xB1 && header[6] == 0x1A && header[7] == 0xE1)
            {
                return InstallerType.Msi;
            }

            // Not MSI, read up to 2MB to search for Inno Setup and Nullsoft signatures
            int bytesRead;
            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(maxBytesToRead);
            try
            {
                if (fs.CanSeek)
                {
                    fs.Position = 0;
                    bytesRead = fs.Read(rentedBuffer, 0, maxBytesToRead);
                }
                else
                {
                    // Fallback for non-seekable streams, preserving the header we already read
                    header.CopyTo(rentedBuffer);
                    int remainingRead = fs.Read(rentedBuffer, 8, maxBytesToRead - 8);
                    bytesRead = headerBytesRead + (remainingRead > 0 ? remainingRead : 0);
                }

                var bufferSpan = rentedBuffer.AsSpan(0, bytesRead);

                if (ContainsIgnoreCase(bufferSpan, "inno setup"u8))
                {
                    return InstallerType.InnoSetup;
                }

                if (ContainsIgnoreCase(bufferSpan, "nullsoft"u8))
                {
                    return InstallerType.Nsis;
                }

                return InstallerType.Unknown;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private static bool ContainsIgnoreCase(ReadOnlySpan<byte> source, ReadOnlySpan<byte> lowerPattern)
    {
        if (lowerPattern.Length == 0) return true;
        if (source.Length < lowerPattern.Length) return false;

        byte firstLower = lowerPattern[0];
        byte firstUpper = (byte)(firstLower - 32);

        int index;
        while ((index = source.IndexOfAny(firstLower, firstUpper)) != -1)
        {
            source = source.Slice(index);
            if (source.Length < lowerPattern.Length)
            {
                return false;
            }

            bool match = true;
            for (int j = 1; j < lowerPattern.Length; j++)
            {
                byte s = source[j];
                if (s >= 65 && s <= 90) // Convert ASCII uppercase to lowercase
                {
                    s = (byte)(s + 32);
                }

                if (s != lowerPattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match) return true;

            source = source.Slice(1);
        }

        return false;
    }
}