namespace PhotoIt.Data;

public static class AlbumPhotoReferenceChecker
{
    public static bool IsReferenced(
        string photoId,
        IEnumerable<Album> albums,
        IEnumerable<ViewableAlbumLayout> layouts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(photoId);
        ArgumentNullException.ThrowIfNull(albums);
        ArgumentNullException.ThrowIfNull(layouts);

        if (albums.Any(album => (album.Chapters ?? []).Any(chapter =>
                string.Equals(chapter.ThumbnailPhotoId, photoId, StringComparison.OrdinalIgnoreCase)
                || (chapter.PhotoIds ?? []).Contains(photoId, StringComparer.OrdinalIgnoreCase))))
            return true;

        return layouts.Any(layout =>
            string.Equals(layout.CoverPhotoId, photoId, StringComparison.OrdinalIgnoreCase)
            || (layout.Pages ?? []).Any(page => (page.Photos ?? []).Any(slot =>
                string.Equals(slot.PhotoId, photoId, StringComparison.OrdinalIgnoreCase))));
    }
}
