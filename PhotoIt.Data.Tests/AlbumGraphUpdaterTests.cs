using PhotoIt.Data;

namespace PhotoIt.Data.Tests;

public sealed class AlbumGraphUpdaterTests
{
    [Fact]
    public void CopyInto_UpdatesOwnedMetadataWithoutChangingTrackedKeys()
    {
        var trackedMetadata = new AlbumMetadataValue
        {
            Id = "venue-id",
            FieldId = "wedding:venue",
            Value = "Garden",
        };
        var removedMetadata = new AlbumMetadataValue
        {
            Id = "removed-id",
            FieldId = "wedding:guest-count",
            Value = "120",
        };
        var trackedChapterMetadata = new AlbumMetadataValue
        {
            Id = "chapter-location-id",
            FieldId = "travel:location",
            Value = "Old place",
        };
        var trackedChapter = new AlbumChapter
        {
            Id = "chapter-id",
            Title = "Old title",
            Metadata = [trackedChapterMetadata],
        };
        var target = new Album
        {
            Id = "album-id",
            Name = "Old name",
            Metadata = [trackedMetadata, removedMetadata],
            Chapters = [trackedChapter],
        };
        var source = new Album
        {
            Id = "album-id",
            Name = "New name",
            Metadata =
            [
                new AlbumMetadataValue
                {
                    Id = "venue-id",
                    FieldId = "wedding:venue",
                    Value = "Hall",
                },
                new AlbumMetadataValue
                {
                    Id = "new-id",
                    FieldId = "wedding:date",
                    Value = "2026-06-14",
                },
            ],
            Chapters =
            [
                new AlbumChapter
                {
                    Id = "chapter-id",
                    Title = "Updated title",
                    Metadata =
                    [
                        new AlbumMetadataValue
                        {
                            Id = "chapter-location-id",
                            FieldId = "travel:location",
                            Value = "New place",
                        },
                    ],
                },
            ],
        };

        AlbumGraphUpdater.CopyInto(target, source);

        Assert.Equal("album-id", target.Id);
        Assert.Equal("New name", target.Name);
        var updatedMetadata = Assert.Single(target.Metadata, item => item.Id == "venue-id");
        Assert.Same(trackedMetadata, updatedMetadata);
        Assert.Equal("Hall", updatedMetadata.Value);
        var addedMetadata = Assert.Single(target.Metadata, item => item.Id == "new-id");
        Assert.Equal("2026-06-14", addedMetadata.Value);
        Assert.DoesNotContain(target.Metadata, item => item.Id == "removed-id");

        var updatedChapter = Assert.Single(target.Chapters);
        Assert.Same(trackedChapter, updatedChapter);
        Assert.Equal("Updated title", updatedChapter.Title);
        var updatedChapterMetadata = Assert.Single(updatedChapter.Metadata);
        Assert.Same(trackedChapterMetadata, updatedChapterMetadata);
        Assert.Equal("New place", updatedChapterMetadata.Value);
    }
}
