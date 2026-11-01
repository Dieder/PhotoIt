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

    public void Upsert(PhotoTagRecord photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        photo.Id = CreateId(photo.Path);
        using var db = _contextFactory.CreateDbContext();
        var existing = db.Set<PhotoTagRecord>().Find(photo.Id);
        if (existing is null)
            db.Set<PhotoTagRecord>().Add(photo);
        else
        {
            db.Entry(existing).CurrentValues.SetValues(photo);
        }

        db.SaveChanges();
    }

    public PhotoTagRecord? FindByPath(string path)
    {
        var id = CreateId(path);
        using var db = _contextFactory.CreateDbContext();
        return db.Set<PhotoTagRecord>().AsNoTracking().FirstOrDefault(photo => photo.Id == id);
    }

    public IEnumerable<PhotoTagRecord> GetAll()
    {
        using var db = _contextFactory.CreateDbContext();
        return db.Set<PhotoTagRecord>().AsNoTracking().ToArray();
    }

    private static string CreateId(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
