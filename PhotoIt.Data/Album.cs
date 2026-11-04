namespace PhotoIt.Data;

public class Album
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string ImageFolderName { get; set; } = string.Empty;
    public BackGroundFill BackGroundFill { get; set; } = new BackGroundFill() { BackroundColor="white"};
    public string Name { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string AlbumType { get; set; } = "travel";
    public List<AlbumMetadataValue> Metadata { get; set; } = [];

    public TripGraph TripGraph { get; set; } = new TripGraph(); 
    public DateTime? TravelStartDate { get; set; }
    public DateTime? TravelEndDate { get; set; }
    public bool ShowRouteOnCover { get; set; } = true;
    public bool UseChapterTitles { get; set; }
    public AlbumChapterMode DefaultChapterMode { get; set; } = AlbumChapterMode.ChapterOnly;
    public List<AlbumChapter> Chapters { get; set; } = [];

    public string Intro { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;  

    public string ShareSlug { get; set; } = string.Empty;
    public int ShareSeed { get; set; }
}
