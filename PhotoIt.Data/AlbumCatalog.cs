using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace PhotoIt.Data;

public sealed class AlbumCatalog
{
    private readonly IPhotoItContextFactory _contextFactory;

    public AlbumCatalog(IPhotoItContextFactory contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public IReadOnlyList<Album> List()
    {
        using var db = _contextFactory.CreateDbContext();
        return db.Set<Album>().AsNoTracking()
            .OrderByDescending(album => album.UpdatedUtc)
            .ToList();
    }

    public Album? Get(string id)
    {
        using var db = _contextFactory.CreateDbContext();
        return db.Set<Album>().AsNoTracking().FirstOrDefault(album => album.Id == id);
    }

    public string[] PhotoIds(string albumId)
    {
        using var db = _contextFactory.CreateDbContext();
        return db.Set<AlbumPhoto>().AsNoTracking()
            .Where(photo => photo.AlbumId == albumId)
            .Select(photo => photo.Id)
            .ToArray();
    }

    public Album? GetShared(string slug, int seed)
    {
        var normalizedSlug = NormalizeShareSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug) || seed <= 0)
            return null;

        using var db = _contextFactory.CreateDbContext();
        return db.Set<Album>().AsNoTracking()
            .FirstOrDefault(album => album.ShareSlug == normalizedSlug && album.ShareSeed == seed);
    }

    public void EnsureShareLink(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);

        var slug = FormatShareSlug(album.UserId);
        if (string.Equals(album.ShareSlug, slug, StringComparison.Ordinal) && album.ShareSeed > 0)
            return;

        album.ShareSlug = slug;
        using var db = _contextFactory.CreateDbContext();
        do
        {
            album.ShareSeed = RandomNumberGenerator.GetInt32(100_000, 1_000_000);
        }
        while (db.Set<Album>().Any(candidate =>
                   candidate.Id != album.Id &&
                   candidate.ShareSlug == album.ShareSlug &&
                   candidate.ShareSeed == album.ShareSeed));

        db.Set<Album>().Update(album);
        db.SaveChanges();
    }

    public bool ImageFolderNameExists(string imageFolderName, string? excludingId = null)
    {
        var normalizedName = NormalizeFolderName(imageFolderName);
        using var db = _contextFactory.CreateDbContext();
        return db.Set<Album>().Any(album =>
            album.ImageFolderName == normalizedName &&
            (excludingId == null || album.Id != excludingId));
    }

    public void Create(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        album.Id = Guid.NewGuid().ToString("N");
        album.CreatedUtc = DateTime.UtcNow;
        album.UpdatedUtc = album.CreatedUtc;
        album.ImageFolderName = $"ALBUM-{album.Id.ToUpperInvariant()}";
        using var db = _contextFactory.CreateDbContext();
        db.Set<Album>().Add(album);
        db.SaveChanges();
    }

    public bool Update(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        album.ImageFolderName = NormalizeFolderName(album.ImageFolderName);
        album.UpdatedUtc = DateTime.UtcNow;
        using var db = _contextFactory.CreateDbContext();
        if (!db.Set<Album>().Any(existing => existing.Id == album.Id))
            return false;

        db.Set<Album>().Update(album);
        db.SaveChanges();
        return true;
    }

    public bool Delete(string id)
    {
        using var db = _contextFactory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var album = db.Set<Album>().FirstOrDefault(candidate => candidate.Id == id);
        if (album is null)
            return false;

        db.Set<AlbumPhoto>().Where(photo => photo.AlbumId == id).ExecuteDelete();
        db.Set<ViewableAlbumLayout>().Where(layout => layout.AlbumId == id).ExecuteDelete();
        db.Remove(album);
        db.SaveChanges();
        transaction.Commit();
        return true;
    }

    public IReadOnlyList<AlbumPhoto> ListPhotos(string albumId)
    {
        using var db = _contextFactory.CreateDbContext();
        var photos = db.Set<AlbumPhoto>().Where(photo => photo.AlbumId == albumId)
            .ToList();
        photos = photos.OrderByDescending(photo => photo.DateTakenUtc ?? photo.UploadedUtc).ToList();

        var changed = false;
        foreach (var photo in photos)
        {
            if (NormalizeLegacyAnalysis(photo))
            {
                db.Update(photo);
                changed = true;
            }
        }

        if (changed)
            db.SaveChanges();

        return photos;
    }

    public static DateTime GetDisplayDate(AlbumPhoto photo)
    {
        return photo.DateTakenUtc.HasValue
            ? photo.DateTakenUtc.Value.ToLocalTime()
            : photo.UploadedUtc.ToLocalTime();
    }

    public static DateTime GetDisplayDate(DateTime? dateTakenUtc, DateTime uploadedUtc)
    {
        return dateTakenUtc.HasValue
            ? dateTakenUtc.Value.ToLocalTime()
            : uploadedUtc.ToLocalTime();
    }

    public void AddPhoto(AlbumPhoto photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        using var db = _contextFactory.CreateDbContext();
        db.Set<AlbumPhoto>().Add(photo);
        db.SaveChanges();
    }

    public AlbumPhoto? GetPhoto(string albumId, string photoId)
    {
        using var db = _contextFactory.CreateDbContext();
        var photo = db.Set<AlbumPhoto>().FirstOrDefault(photo =>
            photo.AlbumId == albumId && photo.Id == photoId);
        if (photo is not null && NormalizeLegacyAnalysis(photo))
            db.SaveChanges();
        return photo;
    }

    public bool DeletePhoto(string id)
    {
        using var db = _contextFactory.CreateDbContext();
        var photo = db.Set<AlbumPhoto>().Find(id);
        if (photo is null)
            return false;

        db.Remove(photo);
        return db.SaveChanges() > 0;
    }

    public bool UpdatePhoto(AlbumPhoto photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        using var db = _contextFactory.CreateDbContext();
        if (!db.Set<AlbumPhoto>().Any(existing => existing.Id == photo.Id))
            return false;

        db.Set<AlbumPhoto>().Update(photo);
        db.SaveChanges();
        return true;
    }

    public void Touch(string albumId)
    {
        using var db = _contextFactory.CreateDbContext();
        var album = db.Set<Album>().Find(albumId);
        if (album is null)
            return;

        album.UpdatedUtc = DateTime.UtcNow;
        db.SaveChanges();
    }

    public ViewableAlbumLayout? GetLayout(string albumId)
    {
        using var db = _contextFactory.CreateDbContext();
        var layout = db.Set<ViewableAlbumLayout>().AsNoTracking()
            .FirstOrDefault(layout => layout.AlbumId == albumId);
        if (layout is not null)
            NormalizeLayout(layout);

        return layout;
    }

    public bool HasLayout(string albumId)
    {
        using var db = _contextFactory.CreateDbContext();
        return db.Set<ViewableAlbumLayout>().Any(layout => layout.AlbumId == albumId);
    }

    public void SaveLayout(ViewableAlbumLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (string.IsNullOrWhiteSpace(layout.AlbumId))
            throw new ArgumentException("AlbumId is required.", nameof(layout));

        NormalizeLayout(layout);
        layout.UpdatedUtc = DateTime.UtcNow;
        using var db = _contextFactory.CreateDbContext();
        if (db.Set<ViewableAlbumLayout>().Any(existing => existing.AlbumId == layout.AlbumId))
            db.Set<ViewableAlbumLayout>().Update(layout);
        else
            db.Set<ViewableAlbumLayout>().Add(layout);

        var album = db.Set<Album>().Find(layout.AlbumId);
        if (album is not null)
            album.UpdatedUtc = DateTime.UtcNow;
        db.SaveChanges();
    }

    public bool DeleteLayout(string albumId)
    {
        using var db = _contextFactory.CreateDbContext();
        var layout = db.Set<ViewableAlbumLayout>().Find(albumId);
        if (layout is null)
            return false;

        db.Remove(layout);
        return db.SaveChanges() > 0;
    }

    private static string NormalizeFolderName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    public void UpdateAlbumBackground(string id, BackGroundFill fill)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(fill);
        using var db = _contextFactory.CreateDbContext();
        var album = db.Set<Album>().Find(id);
        if (album is null)
            throw new ArgumentException("Album not found.", nameof(id));
        album.BackGroundFill = fill;
        album.UpdatedUtc = DateTime.UtcNow;

        db.SaveChanges();
    }

    public static string FormatShareSlug(string value)
    {
        var source = value.Trim();
        var atIndex = source.IndexOf('@');
        if (atIndex > 0)
            source = source[..atIndex];

        var builder = new System.Text.StringBuilder(source.Length);
        var separatorPending = false;
        foreach (var character in source.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (separatorPending && builder.Length > 0)
                    builder.Append('-');

                builder.Append(character);
                separatorPending = false;
            }
            else if (builder.Length > 0)
            {
                separatorPending = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string NormalizeShareSlug(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static void NormalizeLayout(ViewableAlbumLayout layout)
    {
        layout.Pages ??= [];

        foreach (var page in layout.Pages)
        {
            page.Photos ??= [];

            foreach (var slot in page.Photos)
            {
                if (slot.OffsetX is { } offsetX && !double.IsFinite(offsetX))
                    slot.OffsetX = null;

                if (slot.OffsetY is { } offsetY && !double.IsFinite(offsetY))
                    slot.OffsetY = null;

                if (!double.IsFinite(slot.PositionX))
                    slot.PositionX = 50d;
                else
                    slot.PositionX = Math.Clamp(slot.PositionX, 0d, 100d);

                if (!double.IsFinite(slot.PositionY))
                    slot.PositionY = 50d;
                else
                    slot.PositionY = Math.Clamp(slot.PositionY, 0d, 100d);

                if (!double.IsFinite(slot.Zoom))
                    slot.Zoom = 1d;
                else
                    slot.Zoom = Math.Clamp(slot.Zoom, 1d, 3d);
            }
        }
    }

    private static bool NormalizeLegacyAnalysis(AlbumPhoto photo)
    {
        if (photo.Tags.Count > 0 || string.IsNullOrWhiteSpace(photo.Description))
            return false;

        var start = photo.Description.IndexOf('{');
        var end = photo.Description.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(photo.Description[start..(end + 1)]);
            var root = document.RootElement;
            if (!root.TryGetProperty("description", out var descriptionElement) ||
                !root.TryGetProperty("tags", out var tagsElement) ||
                tagsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return false;

            photo.Description = descriptionElement.GetString() ?? string.Empty;
            photo.Tags = tagsElement.EnumerateArray()
                .Select(tag => tag.GetString())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            // Leave malformed legacy analysis untouched for manual re-indexing.
            return false;
        }
    }
}
