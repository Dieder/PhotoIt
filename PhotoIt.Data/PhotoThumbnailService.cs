using ImageMagick;

namespace PhotoIt.Data;

public sealed class PhotoThumbnailService
{
    public const int MaxWidth = 1600;
    public const int MaxHeight = 1200;

    public byte[] Create(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var image = new MagickImage(source);
        image.AutoOrient();

        if (image.Width > MaxWidth || image.Height > MaxHeight)
        {
            var scale = Math.Min(MaxWidth / (double)image.Width, MaxHeight / (double)image.Height);
            image.Resize(
                Math.Max(1, (uint)Math.Round(image.Width * scale)),
                Math.Max(1, (uint)Math.Round(image.Height * scale)));
        }

        image.BackgroundColor = MagickColors.White;
        image.Alpha(AlphaAction.Remove);
        image.Strip();
        image.Quality = 84;
        image.Format = MagickFormat.Jpeg;
        return image.ToByteArray();
    }
}
