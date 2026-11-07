using PhotoIt.Data;

namespace PhotoIt.Data.Tests;

public sealed class LayoutGraphUpdaterTests
{
    [Fact]
    public void CopyInto_UpdatesOwnedValuesWithoutReplacingExistingOwnedEntities()
    {
        var trackedType = new PhotoType { Id = "portrait", Name = "Portrait" };
        var trackedAppearance = new PageAppearance { Title = "Old title" };
        var trackedSlot = new PhotoSlot(1, 1, trackedType, PhotoId: "old-photo");
        var trackedPage = new LayoutPage
        {
            Number = 1,
            ChapterId = "chapter-1",
            Appearance = trackedAppearance,
            Photos = [trackedSlot],
        };
        var target = new ViewableAlbumLayout
        {
            AlbumId = "album-1",
            Title = "Old title",
            Pages = [trackedPage],
        };
        var source = new ViewableAlbumLayout
        {
            AlbumId = "album-1",
            Title = "Updated title",
            Pages =
            [
                new LayoutPage
                {
                    Number = 1,
                    ChapterId = "chapter-1",
                    Appearance = new PageAppearance { Title = "Updated page" },
                    Photos = [new PhotoSlot(1, 1, PhotoType.Landscape, PhotoId: "new-photo")],
                },
            ],
        };

        LayoutGraphUpdater.CopyInto(target, source);

        Assert.Same(trackedPage, Assert.Single(target.Pages));
        Assert.Same(trackedAppearance, trackedPage.Appearance);
        Assert.Equal("Updated page", trackedPage.Appearance.Title);
        Assert.Same(trackedSlot, Assert.Single(trackedPage.Photos));
        Assert.Same(trackedType, trackedSlot.Type);
        Assert.Equal("landscape", trackedSlot.Type.Id);
        Assert.Equal("new-photo", trackedSlot.PhotoId);
        Assert.Equal("Updated title", target.Title);
    }

    [Fact]
    public void CopyInto_ClonesOwnedTypeForNewSlots()
    {
        var source = new ViewableAlbumLayout
        {
            AlbumId = "album-1",
            Pages =
            [
                new LayoutPage
                {
                    Number = 1,
                    ChapterId = "chapter-1",
                    Photos =
                    [
                        new PhotoSlot(1, 1, PhotoType.Landscape),
                        new PhotoSlot(2, 1, PhotoType.Landscape),
                    ],
                },
            ],
        };
        var target = new ViewableAlbumLayout { AlbumId = "album-1" };

        LayoutGraphUpdater.CopyInto(target, source);

        var savedSlots = Assert.Single(target.Pages).Photos;
        Assert.NotSame(savedSlots[0].Type, savedSlots[1].Type);
        Assert.NotSame(PhotoType.Landscape, savedSlots[0].Type);
        Assert.Equal("landscape", savedSlots[0].Type.Id);
    }
}
