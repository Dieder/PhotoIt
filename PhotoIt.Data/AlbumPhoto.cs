using LiteDB;

namespace PhotoIt.Data;

public sealed class AlbumPhoto
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; set; } = string.Empty;  
    public string AlbumId { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long Length { get; set; }
    public int? PixelWidth { get; set; }
    public int? PixelHeight { get; set; }
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DateTakenUtc { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? LensModel { get; set; }
    public string? FocalLength { get; set; }
    public string? FocalLength35Mm { get; set; }
    public string? SubjectDistance { get; set; }
    public string? Flash { get; set; }
    public string? Dimensions { get; set; }
    public string? ExposureTime { get; set; }
    public string? FNumber { get; set; }
    public string? IsoSpeed { get; set; }
    public string? ExposureProgram { get; set; }
    public string? WhiteBalance { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime? IndexedUtc { get; set; }
}
