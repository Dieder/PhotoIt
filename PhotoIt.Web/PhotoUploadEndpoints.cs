using System.Collections.Concurrent;
using PhotoIt.AiOrchestration;
using PhotoIt.Data;
using PhotoIt.ServiceDefaults;
using PhotoIt.Web.Components;

public static class PhotoUploadEndpoints
{
    private const int MaxConcurrentFinalizeOperations = 4;

    public static IEndpointRouteBuilder MapPhotoUploadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/albums/{albumId}/uploads")
            .RequireAuthorization();

        group.MapPost("/plan", CreatePlanAsync);
        group.MapPost("/complete", CompleteAsync);
        return endpoints;
    }

    private static async Task<IResult> CreatePlanAsync(
        string albumId,
        PhotoUploadBatchRequest request,
        HttpContext httpContext,
        AlbumCatalog catalog,
        AzurePhotoStorage storage,
        CancellationToken ct)
    {
        if (!HasAjaxRequestHeader(httpContext.Request))
            return Results.BadRequest("Expected a same-origin upload request.");

        if (request.Files is null || !PhotoUploadRules.IsValidBatchFileCount(request.Files.Count))
            return Results.BadRequest($"Select between 1 and {PhotoUploadRules.MaxBatchFileCount} files.");

        var album = await catalog.GetAsync(albumId, ct);
        if (album is null)
            return Results.NotFound();

        var userId = CurrentUserHelper.GetUserId(httpContext.User);
        if (!string.Equals(userId, album.UserId, StringComparison.Ordinal))
            return Results.Forbid();

        var plannedUploads = new List<PlannedPhotoUpload>(request.Files.Count);
        foreach (var file in request.Files)
        {
            string extension;
            try
            {
                extension = PhotoUploadRules.ValidateFile(file.FileName, file.ContentType, file.Length);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(exception.Message);
            }

            var photoId = Guid.NewGuid().ToString("N");
            var blobName = $"{album.ImageFolderName}/{photoId}{extension}";
            plannedUploads.Add(new PlannedPhotoUpload(
                photoId,
                Path.GetFileName(file.FileName.Replace('\\', '/')),
                blobName,
                PhotoUploadRules.GetContentType(extension),
                file.Length));
        }

        var expiration = DateTimeOffset.UtcNow.AddHours(2);
        var sasUris = await storage.CreateUploadSasUrisAsync(
            plannedUploads.Select(plan => plan.BlobName),
            expiration,
            ct);
        return Results.Ok(plannedUploads.Select(plan => new PhotoUploadTarget(
            plan.PhotoId,
            plan.OriginalFileName,
            plan.BlobName,
            plan.ContentType,
            plan.Length,
            sasUris[plan.BlobName])));
    }

    private static async Task<IResult> CompleteAsync(
        string albumId,
        PhotoUploadCompletionRequest request,
        HttpContext httpContext,
        AlbumCatalog catalog,
        AzurePhotoStorage storage,
        PhotoThumbnailService thumbnails,
        PhotoIndexQueue indexQueue,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("PhotoUploadEndpoints");
        if (!HasAjaxRequestHeader(httpContext.Request))
            return Results.BadRequest("Expected a same-origin upload request.");

        if (request.Files is null || !PhotoUploadRules.IsValidBatchFileCount(request.Files.Count))
            return Results.BadRequest($"A batch can contain at most {PhotoUploadRules.MaxBatchFileCount} files.");

        var album = await catalog.GetAsync(albumId, ct);
        if (album is null)
            return Results.NotFound();

        var userId = CurrentUserHelper.GetUserId(httpContext.User);
        if (!string.Equals(userId, album.UserId, StringComparison.Ordinal))
            return Results.Forbid();

        var completionItems = new List<ValidatedPhotoUpload>(request.Files.Count);
        var uniquePhotoIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in request.Files)
        {
            string extension;
            try
            {
                extension = PhotoUploadRules.ValidateFile(file.OriginalFileName, file.ContentType, file.Length);
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(exception.Message);
            }

            if (!Guid.TryParseExact(file.PhotoId, "N", out _)
                || !uniquePhotoIds.Add(file.PhotoId)
                || !string.Equals(
                    file.BlobName,
                    $"{album.ImageFolderName}/{file.PhotoId}{extension}",
                    StringComparison.Ordinal))
                return Results.BadRequest("The upload list contains an invalid photo reference.");

            completionItems.Add(new ValidatedPhotoUpload(
                file.PhotoId,
                Path.GetFileName(file.OriginalFileName.Replace('\\', '/')),
                file.BlobName,
                PhotoUploadRules.GetContentType(extension),
                file.Length));
        }

        var failures = new ConcurrentBag<string>();
        var successfulUploads = 0;
        using var concurrency = new SemaphoreSlim(MaxConcurrentFinalizeOperations);
        var operations = completionItems.Select(async file =>
        {
            await concurrency.WaitAsync(ct);
            try
            {
                var existing = await catalog.GetPhotoAsync(albumId, file.PhotoId, ct);
                if (existing is not null)
                    throw new InvalidOperationException("This file has already been registered.");

                var properties = await storage.GetPropertiesAsync(file.BlobName, ct);
                if (properties.ContentLength != file.Length)
                    throw new InvalidDataException("The uploaded file size did not match the selected file.");

                var download = await storage.DownloadAsync(file.BlobName, ct);
                await using var content = download.Content;
                using var memory = new MemoryStream(checked((int)file.Length));
                await content.CopyToAsync(memory, ct);
                var bytes = memory.ToArray();
                var isVideo = string.Equals(file.ContentType, "video/mp4", StringComparison.Ordinal);
                var metadata = isVideo ? new AlbumPhotoMetadata() : PhotoMetadataReader.Extract(bytes);
                byte[]? thumbnailBytes = null;
                var thumbnailUnavailable = false;
                if (!isVideo)
                {
                    try
                    {
                        thumbnailBytes = thumbnails.Create(bytes);
                    }
                    catch (ImageMagick.MagickException exception)
                    {
                        thumbnailUnavailable = true;
                        logger.LogWarning(exception, "Could not create photo thumbnail during direct upload finalization. AlbumId: {AlbumId}, MediaId: {MediaId}", albumId, file.PhotoId);
                    }
                }

                var photo = new AlbumPhoto
                {
                    Id = file.PhotoId,
                    UserId = album.UserId,
                    AlbumId = albumId,
                    BlobName = file.BlobName,
                    OriginalFileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    Length = file.Length,
                    ThumbnailUnavailable = thumbnailUnavailable,
                    PixelWidth = metadata.PixelWidth,
                    PixelHeight = metadata.PixelHeight,
                    UploadedUtc = DateTime.UtcNow,
                    DateTakenUtc = metadata.DateTakenUtc,
                    Latitude = metadata.Latitude,
                    Longitude = metadata.Longitude,
                    CameraMake = metadata.CameraMake,
                    CameraModel = metadata.CameraModel,
                    LensModel = metadata.LensModel,
                    FocalLength = metadata.FocalLength,
                    FocalLength35Mm = metadata.FocalLength35Mm,
                    SubjectDistance = metadata.SubjectDistance,
                    Flash = metadata.Flash,
                    Dimensions = metadata.Dimensions,
                    ExposureTime = metadata.ExposureTime,
                    FNumber = metadata.FNumber,
                    IsoSpeed = metadata.IsoSpeed,
                    ExposureProgram = metadata.ExposureProgram,
                    WhiteBalance = metadata.WhiteBalance,
                };

                if (thumbnailBytes is not null)
                {
                    var thumbnailBlobName = $"{album.ImageFolderName}/thumbnails/{photo.Id}.jpg";
                    await storage.UploadAsync(
                        thumbnailBlobName,
                        new MemoryStream(thumbnailBytes),
                        "image/jpeg",
                        ct,
                        overwrite: true);
                    photo.ThumbnailBlobName = thumbnailBlobName;
                }

                await catalog.AddPhotoAsync(photo, ct);
                if (!isVideo)
                    indexQueue.Enqueue(new AlbumPhotoIndexRequest(albumId, photo.Id));
                PhotoItTelemetry.RecordPhotoUploaded();
                Interlocked.Increment(ref successfulUploads);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Could not finalize uploaded media. AlbumId: {AlbumId}, MediaId: {MediaId}", albumId, file.PhotoId);
                failures.Add($"{file.OriginalFileName}: {exception.Message}");
            }
            finally
            {
                concurrency.Release();
            }
        });

        await Task.WhenAll(operations);
        if (successfulUploads > 0)
            await catalog.TouchAsync(albumId, ct);

        return Results.Ok(new PhotoUploadCompletionResult(successfulUploads, failures.ToArray()));
    }

    private static bool HasAjaxRequestHeader(HttpRequest request) =>
        string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

    private sealed record PlannedPhotoUpload(
        string PhotoId,
        string OriginalFileName,
        string BlobName,
        string ContentType,
        long Length);

    private sealed record ValidatedPhotoUpload(
        string PhotoId,
        string OriginalFileName,
        string BlobName,
        string ContentType,
        long Length);
}

public sealed record PhotoUploadFileRequest(string FileName, string ContentType, long Length);
public sealed record PhotoUploadBatchRequest(IReadOnlyList<PhotoUploadFileRequest> Files);
public sealed record PhotoUploadTarget(
    string PhotoId,
    string OriginalFileName,
    string BlobName,
    string ContentType,
    long Length,
    Uri UploadUri);
public sealed record PhotoUploadCompletionFile(
    string PhotoId,
    string OriginalFileName,
    string BlobName,
    string ContentType,
    long Length);
public sealed record PhotoUploadCompletionRequest(IReadOnlyList<PhotoUploadCompletionFile> Files);
public sealed record PhotoUploadCompletionResult(int UploadedCount, IReadOnlyList<string> Failures);
