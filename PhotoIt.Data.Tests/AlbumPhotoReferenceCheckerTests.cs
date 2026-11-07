using PhotoIt.Data;

namespace PhotoIt.Data.Tests;

public sealed class AlbumPhotoReferenceCheckerTests
{
    [Fact]
    public void IsReferenced_checks_album_chapter_selection_and_cover_references()
    {
        var album = new Album
        {
            Chapters = [new AlbumChapter
            {
                PhotoIds = ["chapter-photo"],
                ThumbnailPhotoId = "chapter-thumbnail"
            }]
        };
        var layout = new ViewableAlbumLayout { CoverPhotoId = "cover-photo" };

        Assert.True(AlbumPhotoReferenceChecker.IsReferenced("chapter-photo", [album], [layout]));
        Assert.True(AlbumPhotoReferenceChecker.IsReferenced("chapter-thumbnail", [album], [layout]));
        Assert.True(AlbumPhotoReferenceChecker.IsReferenced("cover-photo", [album], [layout]));
    }

    [Fact]
    public void IsReferenced_checks_chapter_page_photo_assignments()
    {
        var layout = new ViewableAlbumLayout
        {
            Pages = [new LayoutPage { Photos = [new PhotoSlot { PhotoId = "page-photo" }] }]
        };

        Assert.True(AlbumPhotoReferenceChecker.IsReferenced("page-photo", [], [layout]));
    }

    [Fact]
    public void IsReferenced_allows_a_photo_without_album_references()
    {
        Assert.False(AlbumPhotoReferenceChecker.IsReferenced("unused-photo", [], []));
    }
}
