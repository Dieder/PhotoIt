namespace PhotoIt.Data;

public sealed class AlbumChapter
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ThumbnailPhotoId { get; set; }
    public AlbumChapterMode Mode { get; set; } = AlbumChapterMode.ChapterOnly;
    public List<string> PhotoIds { get; set; } = [];
}
