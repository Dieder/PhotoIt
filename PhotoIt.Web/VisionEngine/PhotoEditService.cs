using PhotoIt.Data;
using PhotoIt.ServiceDefaults;

namespace PhotoIt.Web.VisionEngine;

public sealed class PhotoEditException(string message) : Exception(message);

public sealed class PhotoEditService(
    VisionEngineClient client,
    Microsoft.Extensions.Options.IOptions<VisionEngineOptions> options,
    AlbumCatalog catalog,
    AzurePhotoStorage storage,
    PhotoThumbnailService thumbnails,
    ILogger<PhotoEditService> logger)
{
    private const int MaxDimension = 1024;

    public bool IsConfigured => client.IsConfigured;

    public async Task<AlbumPhoto> RunAsync(
        string userId,
        string albumId,
        string photoId,
        ImageTaskOperation operation,
        string prompt,
        IReadOnlyCollection<string> referencePhotoIds,
        Action<string>? progress,
        CancellationToken ct)
    {
        using var activity = PhotoItTelemetry.ActivitySource.StartActivity("photo.edit");
        activity?.SetTag("photo.edit.operation", operation.ToString());
        if (!client.IsConfigured)
            throw new PhotoEditException("AI photo editing is not configured.");

        var promptError = PhotoEditRules.ValidatePrompt(prompt);
        if (promptError is not null)
            throw new PhotoEditException(promptError);

        var definition = PhotoEditOperations.Find(operation)
            ?? throw new PhotoEditException("This operation is not available.");

        var album = await catalog.GetAsync(albumId, ct);
        if (album is null || !string.Equals(album.UserId, userId, StringComparison.Ordinal))
            throw new PhotoEditException("The album could not be found.");

        var source = await catalog.GetPhotoAsync(albumId, photoId, ct);
        if (source is null || source.IsPendingEdit || PhotoType.IsVideo(source))
            throw new PhotoEditException("This photo cannot be edited.");

        var references = new List<AlbumPhoto>();
        foreach (var referenceId in referencePhotoIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var reference = await catalog.GetPhotoAsync(albumId, referenceId, ct)
                ?? throw new PhotoEditException("A reference photo no longer exists in this album.");
            references.Add(reference);
        }

        var referenceError = PhotoEditRules.ValidateReferences(source.Id, references);
        if (referenceError is not null)
            throw new PhotoEditException(referenceError);
        if (definition.NeedsReferences && references.Count == 0)
            throw new PhotoEditException($"'{definition.Name}' needs at least one reference photo.");

        progress?.Invoke("Uploading photos…");
        var inputs = new List<(string, string, byte[])>();
        foreach (var photo in references.Prepend(source))
            inputs.Add((photo.OriginalFileName, photo.ContentType, await ReadBlobAsync(photo.BlobName, ct)));
        var inputReferences = await client.UploadInputsAsync(inputs, ct);

        var (width, height) = FitDimensions(source.PixelWidth, source.PixelHeight);
        progress?.Invoke("Submitting task…");
        var status = await client.SubmitAsync(new ImageTaskSubmission
        {
            Operation = operation,
            Prompt = prompt.Trim(),
            Inputs = inputReferences,
            Width = width,
            Height = height,
        }, ct);

        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));
        var deadline = DateTimeOffset.UtcNow.AddSeconds(Math.Max(10, options.Value.TimeoutSeconds));
        while (status.State is ImageTaskState.Queued or ImageTaskState.Processing)
        {
            progress?.Invoke(status.State == ImageTaskState.Queued ? "Waiting in queue…" : "Editing photo…");
            if (DateTimeOffset.UtcNow > deadline)
                throw new PhotoEditException("The AI edit took too long. Please try again.");

            await Task.Delay(interval, ct);
            status = await client.GetStatusAsync(status.TaskId, ct)
                ?? throw new PhotoEditException("The AI task was lost. Please try again.");
        }

        if (status.State != ImageTaskState.Processed || status.Outputs.Count == 0)
        {
            logger.LogWarning("VisionEngine task {TaskId} ended as {State}: {Reason}", status.TaskId, status.State, status.FailureReason);
            throw new PhotoEditException("The AI could not process this photo. Try a different prompt or operation.");
        }

        progress?.Invoke("Saving result…");
        var output = await client.DownloadOutputAsync(status.TaskId, 0, ct);
        return await StorePendingAsync(album, source, operation, output.Content, output.ContentType, ct);
    }

    public async Task DiscardAsync(string albumId, string pendingPhotoId, CancellationToken ct = default)
    {
        var pending = await catalog.GetPhotoAsync(albumId, pendingPhotoId, ct);
        if (pending is null || !pending.IsPendingEdit)
            return;

        if (!await catalog.DeclineEditAsync(albumId, pendingPhotoId, ct))
            return;

        await storage.DeleteAsync(pending.BlobName, ct);
        if (!string.IsNullOrWhiteSpace(pending.ThumbnailBlobName))
            await storage.DeleteAsync(pending.ThumbnailBlobName, ct);
    }

    private async Task<AlbumPhoto> StorePendingAsync(
        Album album,
        AlbumPhoto source,
        ImageTaskOperation operation,
        byte[] bytes,
        string contentType,
        CancellationToken ct)
    {
        var extension = contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg"
            : contentType.Contains("webp", StringComparison.OrdinalIgnoreCase) ? ".webp"
            : ".png";
        var id = Guid.NewGuid().ToString("N");
        var blobName = $"{album.ImageFolderName}/{id}{extension}";
        var metadata = PhotoMetadataReader.Extract(bytes);
        var photo = new AlbumPhoto
        {
            Id = id,
            UserId = album.UserId,
            AlbumId = album.Id,
            BlobName = blobName,
            OriginalFileName = $"{Path.GetFileNameWithoutExtension(source.OriginalFileName)}-edit{extension}",
            ContentType = contentType,
            Length = bytes.Length,
            PixelWidth = metadata.PixelWidth,
            PixelHeight = metadata.PixelHeight,
            UploadedUtc = DateTime.UtcNow,
            DateTakenUtc = source.DateTakenUtc,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            IsPendingEdit = true,
            EditSourcePhotoId = source.Id,
            EditOperation = operation.ToString(),
        };

        await storage.UploadAsync(blobName, new MemoryStream(bytes), contentType, ct, overwrite: true);
        try
        {
            var thumbnailBlobName = $"{album.ImageFolderName}/thumbnails/{id}.jpg";
            await storage.UploadAsync(thumbnailBlobName, new MemoryStream(thumbnails.Create(bytes)), "image/jpeg", ct, overwrite: true);
            photo.ThumbnailBlobName = thumbnailBlobName;
        }
        catch (ImageMagick.MagickException exception)
        {
            photo.ThumbnailUnavailable = true;
            logger.LogWarning(exception, "Could not create a thumbnail for AI edit result {PhotoId}", id);
        }

        await catalog.AddPhotoAsync(photo, ct);
        return photo;
    }

    private async Task<byte[]> ReadBlobAsync(string blobName, CancellationToken ct)
    {
        var download = await storage.DownloadAsync(blobName, ct);
        await using var content = download.Content;
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, ct);
        return memory.ToArray();
    }

    internal static (int Width, int Height) FitDimensions(int? width, int? height)
    {
        if (width is not > 0 || height is not > 0)
            return (MaxDimension, MaxDimension);

        var scale = Math.Min(1d, (double)MaxDimension / Math.Max(width.Value, height.Value));
        return (Math.Max(64, (int)Math.Round(width.Value * scale)), Math.Max(64, (int)Math.Round(height.Value * scale)));
    }
}
