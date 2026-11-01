using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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

        var applicationUser = modelBuilder.Entity<ApplicationUser>().Metadata;
        foreach (var index in applicationUser.GetIndexes().ToArray())
            applicationUser.RemoveIndex(index);

        var identityRole = modelBuilder.Entity<IdentityRole>().Metadata;
        foreach (var index in identityRole.GetIndexes().ToArray())
            identityRole.RemoveIndex(index);

        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(album => album.Id);
            entity.ToContainer("Albums");
            entity.HasPartitionKey(album => album.UserId);
            entity.OwnsOne(album => album.BackGroundFill);
            entity.OwnsOne(album => album.TripGraph, trip =>
                trip.OwnsMany(graph => graph.Legs, leg =>
                {
                    leg.OwnsOne(routeLeg => routeLeg.From);
                    leg.OwnsOne(routeLeg => routeLeg.To);
                }));
            entity.OwnsMany(album => album.Chapters);
        });

        modelBuilder.Entity<AlbumPhoto>(entity =>
        {
            entity.ToContainer("AlbumPhotos");
            entity.HasPartitionKey(photo => photo.AlbumId);
        });

        modelBuilder.Entity<ViewableAlbumLayout>(entity =>
        {
            entity.HasKey(layout => layout.AlbumId);
            entity.ToContainer("ViewableAlbumLayouts");
            entity.HasPartitionKey(layout => layout.AlbumId);
            entity.OwnsMany(layout => layout.Pages, page =>
            {
                page.OwnsMany(layoutPage => layoutPage.Photos, slot =>
                    slot.OwnsOne(photoSlot => photoSlot.Type));
                page.OwnsOne(layoutPage => layoutPage.Appearance);
            });
        });

        modelBuilder.Entity<PhotoTagRecord>(entity =>
        {
            entity.ToContainer("PhotoTags");
            entity.HasPartitionKey(photo => photo.Id);
        });

        modelBuilder.Entity<IdentityUserPasskey<string>>(entity =>
        {
            entity.HasKey(passkey => new { passkey.UserId, passkey.CredentialId });
            entity.ToContainer("AspNetUserPasskeys");
            entity.HasPartitionKey(passkey => passkey.UserId);
            entity.HasShadowId();
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToContainer("AspNetUsers");
            entity.HasPartitionKey(user => user.Id);
            entity.Property(user => user.ConcurrencyStamp).IsConcurrencyToken(false);
            entity.Property<string>("ETag").IsETagConcurrency();
        });
        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.ToContainer("AspNetRoles");
            entity.HasPartitionKey(role => role.Id);
            entity.Property(role => role.ConcurrencyStamp).IsConcurrencyToken(false);
            entity.Property<string>("ETag").IsETagConcurrency();
        });
        modelBuilder.Entity<IdentityUserClaim<string>>().ToContainer("AspNetUserClaims")
            .HasPartitionKey(claim => claim.UserId)
            .HasShadowId();
        modelBuilder.Entity<IdentityUserLogin<string>>().ToContainer("AspNetUserLogins")
            .HasPartitionKey(login => login.UserId)
            .HasShadowId();
        modelBuilder.Entity<IdentityUserRole<string>>().ToContainer("AspNetUserRoles")
            .HasPartitionKey(userRole => userRole.UserId)
            .HasShadowId();
        modelBuilder.Entity<IdentityUserToken<string>>().ToContainer("AspNetUserTokens")
            .HasPartitionKey(token => token.UserId)
            .HasShadowId();
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToContainer("AspNetRoleClaims")
            .HasPartitionKey(claim => claim.RoleId)
            .HasShadowId();
    }
}

public sealed class AlbumWebContextFactory(IDbContextFactory<AlbumWebContext> contextFactory) : IPhotoItContextFactory
{
    public DbContext CreateDbContext() => contextFactory.CreateDbContext();
}
