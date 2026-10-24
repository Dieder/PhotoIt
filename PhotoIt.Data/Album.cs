using LiteDB;

namespace PhotoIt.Data;

public class Album
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string ImageFolderName { get; set; } = string.Empty;
    public BackGroundFill BackGroundFill { get; set; } = new BackGroundFill() { BackroundColor="white"};
    public string Name { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;

    public TripGraph TripGraph { get; set; } = new TripGraph(); 

    public string Intro { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;  

    public string ShareSlug { get; set; } = string.Empty;
    public int ShareSeed { get; set; }
}
