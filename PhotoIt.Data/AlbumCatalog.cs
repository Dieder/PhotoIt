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

    public async Task<IReadOnlyList<Album>> ListAsync(CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<Album>().AsNoTracking()
            .OrderByDescending(album => album.UpdatedUtc)
            .ToListAsync(ct);
    }

    public async Task<Album?> GetAsync(string id, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<Album>().AsNoTracking().FirstOrDefaultAsync(album => album.Id == id, ct);
    }

    public async Task<string[]> PhotoIdsAsync(string albumId, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<AlbumPhoto>().AsNoTracking()
            .Where(photo => photo.AlbumId == albumId)
            .Select(photo => photo.Id)
            .ToArrayAsync(ct);
    }

    public async Task<Album?> GetSharedAsync(string slug, int seed, CancellationToken ct = default)
    {
        var normalizedSlug = NormalizeShareSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug) || seed <= 0)
            return null;

        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<Album>().AsNoTracking()
            .FirstOrDefaultAsync(album => album.ShareSlug == normalizedSlug && album.ShareSeed == seed, ct);
    }

    public async Task EnsureShareLinkAsync(Album album, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(album);

        var slug = FormatShareSlug(album.UserId);
        if (string.Equals(album.ShareSlug, slug, StringComparison.Ordinal) && album.ShareSeed > 0)
            return;

        album.ShareSlug = slug;
        await using var db = _contextFactory.CreateDbContext();
        album.ShareSeed = RandomNumberGenerator.GetInt32(1, int.MaxValue);

        db.Set<Album>().Update(album);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ImageFolderNameExistsAsync(string imageFolderName, CancellationToken ct = default)
    {
        var normalizedName = NormalizeFolderName(imageFolderName);
        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<Album>()
            .FirstOrDefaultAsync(album => album.ImageFolderName == normalizedName, ct) is not null;
    }

    public async Task CreateAsync(Album album, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(album);
        album.Id = Guid.NewGuid().ToString("N");
        album.CreatedUtc = DateTime.UtcNow;
        album.UpdatedUtc = album.CreatedUtc;
        album.ImageFolderName = $"ALBUM-{album.Id.ToUpperInvariant()}";
        await using var db = _contextFactory.CreateDbContext();
        db.Set<Album>().Add(album);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(Album album, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(album);
        album.ImageFolderName = NormalizeFolderName(album.ImageFolderName);
        album.UpdatedUtc = DateTime.UtcNow;
        await using var db = _contextFactory.CreateDbContext();
        var existing = await db.Set<Album>()
            .Where(existing => existing.UserId == album.UserId)
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == album.Id, ct);
        if (existing is null)
            return false;

        var layout = await db.Set<ViewableAlbumLayout>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.AlbumId == album.Id, ct);
        if (layout is not null)
        {
            var albumPhotoIds = await db.Set<AlbumPhoto>().AsNoTracking()
                .Where(photo => photo.AlbumId == album.Id)
                .Select(photo => photo.Id)
                .ToListAsync(ct);
            AlbumChapterPageRules.Validate(
                album,
                layout.Pages,
                albumPhotoIds.ToHashSet(StringComparer.OrdinalIgnoreCase),
                layout.Pages.Count(page => string.IsNullOrWhiteSpace(page.ChapterId)));
        }

        db.Set<Album>().Update(album);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var album = await db.Set<Album>().FirstOrDefaultAsync(candidate => candidate.Id == id, ct);
        if (album is null)
            return false;

        var photos = await db.Set<AlbumPhoto>().Where(photo => photo.AlbumId == id).ToListAsync(ct);
        var layout = await db.Set<ViewableAlbumLayout>().FirstOrDefaultAsync(layout => layout.AlbumId == id, ct);
        db.RemoveRange(photos);
        if (layout is not null)
            db.Remove(layout);
        db.Remove(album);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AlbumPhoto>> ListPhotosAsync(string albumId, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var photos = await db.Set<AlbumPhoto>().Where(photo => photo.AlbumId == albumId)
            .ToListAsync(ct);
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
            await db.SaveChangesAsync(ct);

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

    public async Task AddPhotoAsync(AlbumPhoto photo, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(photo);
        await using var db = _contextFactory.CreateDbContext();
        db.Set<AlbumPhoto>().Add(photo);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AlbumPhoto?> GetPhotoAsync(string albumId, string photoId, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var photo = await db.Set<AlbumPhoto>().FirstOrDefaultAsync(photo =>
            photo.AlbumId == albumId && photo.Id == photoId, ct);
        if (photo is not null && NormalizeLegacyAnalysis(photo))
            await db.SaveChangesAsync(ct);
        return photo;
    }

    public async Task<bool> DeletePhotoAsync(string id, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var photo = await db.Set<AlbumPhoto>().FirstOrDefaultAsync(existing => existing.Id == id, ct);
        if (photo is null || await IsPhotoReferencedAsync(id, ct))
            return false;

        db.Remove(photo);
        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> IsPhotoReferencedAsync(string photoId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(photoId);

        await using var db = _contextFactory.CreateDbContext();
        var albums = await db.Set<Album>().AsNoTracking().ToListAsync(ct);
        var layouts = await db.Set<ViewableAlbumLayout>().AsNoTracking().ToListAsync(ct);
        return AlbumPhotoReferenceChecker.IsReferenced(photoId, albums, layouts);
    }

    public async Task<bool> UpdatePhotoAsync(AlbumPhoto photo, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(photo);
        await using var db = _contextFactory.CreateDbContext();
        var existing = await db.Set<AlbumPhoto>()
            .Where(existing => existing.AlbumId == photo.AlbumId)
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == photo.Id, ct);
        if (existing is null)
            return false;

        db.Set<AlbumPhoto>().Update(photo);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task TouchAsync(string albumId, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var album = await db.Set<Album>().FirstOrDefaultAsync(album => album.Id == albumId, ct);
        if (album is null)
            return;

        album.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ViewableAlbumLayout?> GetLayoutAsync(string albumId, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var layout = await db.Set<ViewableAlbumLayout>().AsNoTracking()
            .FirstOrDefaultAsync(layout => layout.AlbumId == albumId, ct);
        if (layout is not null)
            NormalizeLayout(layout);

        return layout;
    }

    public async Task<bool> HasLayoutAsync(string albumId, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<ViewableAlbumLayout>()
            .Where(layout => layout.AlbumId == albumId)
            .FirstOrDefaultAsync(layout => layout.AlbumId == albumId, ct) is not null;
    }

    public async Task SaveLayoutAsync(ViewableAlbumLayout layout, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (string.IsNullOrWhiteSpace(layout.AlbumId))
            throw new ArgumentException("AlbumId is required.", nameof(layout));

        NormalizeLayout(layout);
        layout.UpdatedUtc = DateTime.UtcNow;
        await using var db = _contextFactory.CreateDbContext();
        var existing = await db.Set<ViewableAlbumLayout>()
            .Where(candidate => candidate.AlbumId == layout.AlbumId)
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.AlbumId == layout.AlbumId, ct);
        var albumForValidation = await db.Set<Album>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == layout.AlbumId, ct);
        if (albumForValidation is null)
            throw new InvalidOperationException("The album for this layout could not be found.");

        var albumPhotoIds = await db.Set<AlbumPhoto>().AsNoTracking()
            .Where(photo => photo.AlbumId == layout.AlbumId)
            .Select(photo => photo.Id)
            .ToListAsync(ct);
        AlbumChapterPageRules.Validate(
            albumForValidation,
            layout.Pages,
            albumPhotoIds.ToHashSet(StringComparer.OrdinalIgnoreCase),
            existing?.Pages.Count(page => string.IsNullOrWhiteSpace(page.ChapterId)) ?? 0);

        if (existing is not null)
            db.Set<ViewableAlbumLayout>().Update(layout);
        else
            db.Set<ViewableAlbumLayout>().Add(layout);

        var album = await db.Set<Album>().FirstOrDefaultAsync(album => album.Id == layout.AlbumId, ct);
        if (album is not null)
            album.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteLayoutAsync(string albumId, CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var layout = await db.Set<ViewableAlbumLayout>().FirstOrDefaultAsync(layout => layout.AlbumId == albumId, ct);
        if (layout is null)
            return false;

        db.Remove(layout);
        return await db.SaveChangesAsync(ct) > 0;
    }

    private static string NormalizeFolderName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    public async Task UpdateAlbumBackgroundAsync(string id, BackGroundFill fill, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(fill);
        await using var db = _contextFactory.CreateDbContext();
        var album = await db.Set<Album>().FirstOrDefaultAsync(album => album.Id == id, ct);
        if (album is null)
            throw new ArgumentException("Album not found.", nameof(id));
        album.BackGroundFill = fill;
        album.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
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
