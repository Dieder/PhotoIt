namespace PhotoIt.Data;

public static class PhotoUploadRules
{
    public const int MaxBatchFileCount = 1000;
    public const long MaxFileLength = 100L * 1024 * 1024;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".rw2"
    };

    public static bool IsValidBatchFileCount(int count) => count is > 0 and <= MaxBatchFileCount;

    public static string ValidateFile(string fileName, string? contentType, long length)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (length <= 0 || length > MaxFileLength)
            throw new InvalidOperationException($"Each file must be between 1 byte and {MaxFileLength / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(fileName);
        if (!ImageExtensions.Contains(extension)
            && !string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"File type '{extension}' is not supported.");

        if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(contentType)
                && !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("MP4 files must have a video content type.");
        }
        else if (!string.IsNullOrWhiteSpace(contentType)
            && !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Photo files must have an image content type.");
        }

        return extension.ToLowerInvariant();
    }

    public static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".tif" or ".tiff" => "image/tiff",
        ".webp" => "image/webp",
        ".rw2" => "image/x-panasonic-rw2",
        ".mp4" => "video/mp4",
        _ => throw new InvalidOperationException($"File type '{extension}' is not supported.")
    };
}
