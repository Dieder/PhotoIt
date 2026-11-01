using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PhotoIt.ServiceDefaults;

public static class PhotoItTelemetry
{
    public const string InstrumentationName = "PhotoIt";

    public static readonly ActivitySource ActivitySource = new(InstrumentationName);
    public static readonly Meter Meter = new(InstrumentationName);

    private static readonly Counter<long> LoginAttempts =
        Meter.CreateCounter<long>("photoit.auth.login.attempts");
    private static readonly Counter<long> AlbumsCreated =
        Meter.CreateCounter<long>("photoit.albums.created");
    private static readonly Counter<long> AlbumSaves =
        Meter.CreateCounter<long>("photoit.albums.saved");
    private static readonly Counter<long> AlbumsDeleted =
        Meter.CreateCounter<long>("photoit.albums.deleted");
    private static readonly Counter<long> SharesCreated =
        Meter.CreateCounter<long>("photoit.albums.shares.created");
    private static readonly Counter<long> PhotosUploaded =
        Meter.CreateCounter<long>("photoit.photos.uploaded");
    private static readonly Counter<long> PhotoUploadFailures =
        Meter.CreateCounter<long>("photoit.photos.upload.failures");

    private static readonly object UploadFailureLock = new();
    private static DateOnly uploadFailureDate = DateOnly.FromDateTime(DateTime.UtcNow);
    private static long uploadFailuresToday;

    public static void RecordLoginAttempt(string result) =>
        LoginAttempts.Add(1, new KeyValuePair<string, object?>("result", result));

    public static void RecordAlbumCreated() => AlbumsCreated.Add(1);

    public static void RecordAlbumSave(bool succeeded) =>
        AlbumSaves.Add(1, new KeyValuePair<string, object?>("result", succeeded ? "success" : "failure"));

    public static void RecordAlbumDeleted() => AlbumsDeleted.Add(1);

    public static void RecordShareCreated() => SharesCreated.Add(1);

    public static void RecordPhotoUploaded() => PhotosUploaded.Add(1);

    public static void RecordPhotoUploadFailure()
    {
        PhotoUploadFailures.Add(1);
        lock (UploadFailureLock)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (uploadFailureDate != today)
            {
                uploadFailureDate = today;
                uploadFailuresToday = 0;
            }

            uploadFailuresToday++;
        }
    }

    public static long GetPhotoUploadFailuresToday()
    {
        lock (UploadFailureLock)
        {
            if (uploadFailureDate != DateOnly.FromDateTime(DateTime.UtcNow))
            {
                uploadFailureDate = DateOnly.FromDateTime(DateTime.UtcNow);
                uploadFailuresToday = 0;
            }

            return uploadFailuresToday;
        }
    }
}
