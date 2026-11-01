using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PhotoIt.Data;
using PhotoIt.Web.Data;

public class AlbumWebContext(DbContextOptions<AlbumWebContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<AlbumPhoto> AlbumPhotos => Set<AlbumPhoto>();
    public DbSet<ViewableAlbumLayout> ViewableAlbumLayouts => Set<ViewableAlbumLayout>();
    public DbSet<PhotoTagRecord> PhotoTags => Set<PhotoTagRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(album => album.Id);
            entity.HasIndex(album => album.ImageFolderName).IsUnique();
            entity.HasIndex(album => album.Name);
            entity.Property(album => album.TravelStartDate).HasColumnType("date");
            entity.Property(album => album.TravelEndDate).HasColumnType("date");
            entity.Property(album => album.BackGroundFill)
                .HasConversion(JsonConverter<BackGroundFill>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(JsonComparer<BackGroundFill>());
            entity.Property(album => album.TripGraph)
                .HasConversion(JsonConverter<TripGraph>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(JsonComparer<TripGraph>());
            entity.Property(album => album.Chapters)
                .HasConversion(JsonConverter<List<AlbumChapter>>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(JsonComparer<List<AlbumChapter>>());
        });

        modelBuilder.Entity<AlbumPhoto>(entity =>
        {
            entity.HasKey(photo => photo.Id);
            entity.HasIndex(photo => photo.AlbumId);
            entity.Property(photo => photo.Tags)
                .HasConversion(JsonConverter<List<string>>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(JsonComparer<List<string>>());
            entity.HasOne<Album>()
                .WithMany()
                .HasForeignKey(photo => photo.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ViewableAlbumLayout>(entity =>
        {
            entity.HasKey(layout => layout.AlbumId);
            entity.Property(layout => layout.Pages)
                .HasConversion(JsonConverter<List<LayoutPage>>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(JsonComparer<List<LayoutPage>>());
            entity.HasOne<Album>()
                .WithOne()
                .HasForeignKey<ViewableAlbumLayout>(layout => layout.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhotoTagRecord>(entity =>
        {
            entity.HasKey(photo => photo.Id);
            entity.HasIndex(photo => photo.Path).IsUnique();
            entity.Property(photo => photo.Tags)
                .HasConversion(JsonConverter<List<string>>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(JsonComparer<List<string>>());
        });

        modelBuilder.Entity<IdentityUserPasskey<string>>(entity =>
        {
            entity.ToTable("AspNetUserPasskeys");
            entity.HasKey(passkey => passkey.CredentialId);
            entity.Property(passkey => passkey.CredentialId).HasMaxLength(1024);
            entity.Property(passkey => passkey.UserId).IsRequired();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(passkey => passkey.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.OwnsOne(passkey => passkey.Data, data => data.ToJson());
        });
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static ValueConverter<T, string> JsonConverter<T>() where T : class
    {
        return new ValueConverter<T, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            json => JsonSerializer.Deserialize<T>(json, JsonOptions)!);
    }

    private static ValueComparer<T> JsonComparer<T>() where T : class
    {
        return new ValueComparer<T>(
            (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
            value => JsonSerializer.Serialize(value, JsonOptions).GetHashCode(),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!);
    }
}

public sealed class AlbumWebContextFactory(IDbContextFactory<AlbumWebContext> contextFactory) : IPhotoItContextFactory
{
    public DbContext CreateDbContext() => contextFactory.CreateDbContext();
}

public sealed class AlbumWebContextDesignFactory : IDesignTimeDbContextFactory<AlbumWebContext>
{
    public AlbumWebContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AlbumWebContext");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set ConnectionStrings__AlbumWebContext when creating EF Core migrations.");

        var options = new DbContextOptionsBuilder<AlbumWebContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AlbumWebContext(options);
    }
}
