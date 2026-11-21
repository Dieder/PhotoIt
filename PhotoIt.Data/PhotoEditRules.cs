namespace PhotoIt.Data;

public static class PhotoEditRules
{
    public const int MaxPromptLength = 1024;
    public const int MaxReferencePhotos = 4;

    public static string? ValidatePrompt(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return "Enter a prompt describing the edit.";

        return prompt.Trim().Length > MaxPromptLength
            ? $"The prompt can be at most {MaxPromptLength} characters."
            : null;
    }

    public static string? ValidateReferences(
        string sourcePhotoId,
        IReadOnlyCollection<AlbumPhoto> references)
    {
        if (references.Count > MaxReferencePhotos)
            return $"Select at most {MaxReferencePhotos} reference photos.";

        foreach (var reference in references)
        {
            if (string.Equals(reference.Id, sourcePhotoId, StringComparison.OrdinalIgnoreCase))
                return "The photo being edited cannot be its own reference.";
            if (reference.IsPendingEdit)
                return "A reference photo cannot be an unaccepted edit result.";
            if (PhotoType.IsVideo(reference))
                return "Videos cannot be used as reference photos.";
        }

        return null;
    }

    /// <summary>
    /// Swaps the accepted photo into the slot the editor was opened from. The slot must still hold the original photo.
    /// Photos placed on chapter pages are added to the chapter's selection so the layout stays valid.
    /// </summary>
    public static bool TryReplaceSlot(
        Album album,
        ViewableAlbumLayout layout,
        int pageNumber,
        int slotIndex,
        string originalPhotoId,
        string newPhotoId)
    {
        ArgumentNullException.ThrowIfNull(album);
        ArgumentNullException.ThrowIfNull(layout);

        var page = layout.Pages.FirstOrDefault(candidate => candidate.Number == pageNumber);
        if (page is null || slotIndex < 0 || slotIndex >= page.Photos.Count)
            return false;

        var slot = page.Photos[slotIndex];
        if (!string.Equals(slot.PhotoId, originalPhotoId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(page.ChapterId))
        {
            var chapter = album.Chapters?.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, page.ChapterId, StringComparison.OrdinalIgnoreCase));
            if (chapter is null)
                return false;

            chapter.PhotoIds ??= [];
            if (!chapter.PhotoIds.Contains(newPhotoId, StringComparer.OrdinalIgnoreCase))
                chapter.PhotoIds.Add(newPhotoId);
        }

        slot.PhotoId = newPhotoId;
        return true;
    }
}
