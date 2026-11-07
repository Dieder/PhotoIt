using Microsoft.EntityFrameworkCore;

namespace PhotoIt.Data;

public sealed class PhotoTagRecord
{
    public string Id { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public long Size { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public DateTime IndexedUtc { get; set; }
}

public sealed class PhotoTagCatalog
{
    private readonly IPhotoItContextFactory _contextFactory;

    public PhotoTagCatalog(IPhotoItContextFactory contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task UpsertAsync(PhotoTagRecord photo, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(photo);
        photo.Id = CreateId(photo.Path);
        await using var db = _contextFactory.CreateDbContext();
        var existing = await db.Set<PhotoTagRecord>().FirstOrDefaultAsync(existing => existing.Id == photo.Id, ct);
        if (existing is null)
            db.Set<PhotoTagRecord>().Add(photo);
        else
        {
            db.Entry(existing).CurrentValues.SetValues(photo);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<PhotoTagRecord?> FindByPathAsync(string path, CancellationToken ct = default)
    {
        var id = CreateId(path);
        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<PhotoTagRecord>().AsNoTracking()
            .FirstOrDefaultAsync(photo => photo.Id == id, ct);
    }

    public async Task<IReadOnlyList<PhotoTagRecord>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        return await db.Set<PhotoTagRecord>().AsNoTracking().ToArrayAsync(ct);
    }

    private static string CreateId(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
