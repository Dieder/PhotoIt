namespace PhotoIt.Data;

public static class AlbumChapterPageRules
{
    public static void Validate(
        Album album,
        IEnumerable<LayoutPage> pages,
        IReadOnlySet<string> albumPhotoIds,
        int maxLegacyPageCount = 0)
    {
        ArgumentNullException.ThrowIfNull(album);
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(albumPhotoIds);

        var pageList = pages.ToList();
        if (pageList.Count(page => string.IsNullOrWhiteSpace(page.ChapterId)) > maxLegacyPageCount)
            throw new InvalidOperationException("Every new page must be assigned to a chapter.");

        foreach (var page in pageList.Where(page => !string.IsNullOrWhiteSpace(page.ChapterId)))
        {
            var chapter = album.Chapters?.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, page.ChapterId, StringComparison.OrdinalIgnoreCase));
            if (chapter is null)
                throw new InvalidOperationException($"Page {page.Number} references a chapter that does not exist.");

            if (chapter.Mode != AlbumChapterMode.ChapterWithPages)
                throw new InvalidOperationException($"Page {page.Number} is assigned to a chapter that does not use pages.");

            var chapterPhotoIds = (chapter.PhotoIds ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var photoId in (page.Photos ?? [])
                         .Select(slot => slot.PhotoId)
                         .Where(photoId => !string.IsNullOrWhiteSpace(photoId)))
            {
                if (!albumPhotoIds.Contains(photoId!))
                    throw new InvalidOperationException($"Page {page.Number} contains a photo that does not belong to this album.");

                if (!chapterPhotoIds.Contains(photoId!))
                    throw new InvalidOperationException($"Page {page.Number} contains a photo that is not selected for its chapter.");
            }
        }
    }
}
