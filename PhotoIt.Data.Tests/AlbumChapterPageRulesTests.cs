using PhotoIt.Data;

namespace PhotoIt.Data.Tests;

public sealed class AlbumChapterPageRulesTests
{
    [Fact]
    public void Validate_allows_page_photos_selected_for_its_chapter()
    {
        var chapter = new AlbumChapter
        {
            Id = "chapter-1",
            Mode = AlbumChapterMode.ChapterWithPages,
            PhotoIds = ["photo-1"]
        };
        var album = new Album { Chapters = [chapter] };
        var page = new LayoutPage
        {
            Number = 1,
            ChapterId = chapter.Id,
            Photos = [new PhotoSlot { PhotoId = "photo-1" }]
        };

        AlbumChapterPageRules.Validate(album, [page], new HashSet<string>(["photo-1"]));
    }

    [Fact]
    public void Validate_rejects_photo_not_selected_for_page_chapter()
    {
        var chapter = new AlbumChapter
        {
            Id = "chapter-1",
            Mode = AlbumChapterMode.ChapterWithPages,
            PhotoIds = ["photo-1"]
        };
        var album = new Album { Chapters = [chapter] };
        var page = new LayoutPage
        {
            Number = 1,
            ChapterId = chapter.Id,
            Photos = [new PhotoSlot { PhotoId = "photo-2" }]
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AlbumChapterPageRules.Validate(album, [page], new HashSet<string>(["photo-1", "photo-2"])));

        Assert.Contains("not selected for its chapter", exception.Message);
    }

    [Fact]
    public void Validate_rejects_photo_missing_from_the_album()
    {
        var chapter = new AlbumChapter
        {
            Id = "chapter-1",
            Mode = AlbumChapterMode.ChapterWithPages,
            PhotoIds = ["missing-photo"]
        };
        var page = new LayoutPage
        {
            Number = 1,
            ChapterId = chapter.Id,
            Photos = [new PhotoSlot { PhotoId = "missing-photo" }]
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AlbumChapterPageRules.Validate(new Album { Chapters = [chapter] }, [page], new HashSet<string>()));

        Assert.Contains("does not belong to this album", exception.Message);
    }

    [Fact]
    public void Validate_rejects_page_for_chapter_without_page_mode()
    {
        var chapter = new AlbumChapter { Id = "chapter-1" };
        var page = new LayoutPage { Number = 1, ChapterId = chapter.Id };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AlbumChapterPageRules.Validate(new Album { Chapters = [chapter] }, [page], new HashSet<string>()));

        Assert.Contains("does not use pages", exception.Message);
    }

    [Fact]
    public void Validate_preserves_legacy_pages_without_chapter_assignment()
    {
        var legacyPage = new LayoutPage
        {
            Number = 1,
            Photos = [new PhotoSlot { PhotoId = "legacy-photo" }]
        };

        AlbumChapterPageRules.Validate(new Album(), [legacyPage], new HashSet<string>(), maxLegacyPageCount: 1);
    }

    [Fact]
    public void Validate_rejects_new_page_without_chapter_assignment()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AlbumChapterPageRules.Validate(new Album(), [new LayoutPage()], new HashSet<string>()));

        Assert.Contains("assigned to a chapter", exception.Message);
    }

    [Fact]
    public void RemovePhotoFromIncompatibleChapterPages_clears_only_pages_whose_chapter_does_not_contain_photo()
    {
        var firstChapter = new AlbumChapter { Id = "chapter-1", PhotoIds = ["photo-1"] };
        var secondChapter = new AlbumChapter { Id = "chapter-2", PhotoIds = ["photo-2"] };
        var firstPage = new LayoutPage
        {
            Number = 1,
            ChapterId = firstChapter.Id,
            Photos = [new PhotoSlot { PhotoId = "photo-1" }],
        };
        var secondPage = new LayoutPage
        {
            Number = 3,
            ChapterId = secondChapter.Id,
            Photos =
            [
                new PhotoSlot { Col = 1, Row = 1, PhotoId = "photo-1" },
                new PhotoSlot { Col = 2, Row = 1, PhotoId = "photo-2" },
            ],
        };

        AlbumChapterPageRules.RemovePhotoFromIncompatibleChapterPages(
            [firstPage, secondPage],
            [firstChapter, secondChapter],
            "photo-1");

        Assert.Equal("photo-1", Assert.Single(firstPage.Photos).PhotoId);
        Assert.Null(secondPage.Photos[0].PhotoId);
        Assert.Equal("photo-2", secondPage.Photos[1].PhotoId);
    }
}
