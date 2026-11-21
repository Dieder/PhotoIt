namespace PhotoIt.Data.Tests;

public sealed class PhotoEditRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void ValidatePrompt_rejects_empty_prompt(string? prompt) =>
        Assert.NotNull(PhotoEditRules.ValidatePrompt(prompt));

    [Fact]
    public void ValidatePrompt_enforces_max_length()
    {
        Assert.Null(PhotoEditRules.ValidatePrompt(new string('a', PhotoEditRules.MaxPromptLength)));
        Assert.NotNull(PhotoEditRules.ValidatePrompt(new string('a', PhotoEditRules.MaxPromptLength + 1)));
    }

    [Fact]
    public void ValidateReferences_rejects_self_pending_and_too_many()
    {
        var other = new AlbumPhoto { Id = "b", ContentType = "image/jpeg" };
        Assert.Null(PhotoEditRules.ValidateReferences("a", [other]));
        Assert.NotNull(PhotoEditRules.ValidateReferences("b", [other]));
        Assert.NotNull(PhotoEditRules.ValidateReferences("a", [new AlbumPhoto { Id = "c", IsPendingEdit = true }]));

        var many = Enumerable.Range(0, PhotoEditRules.MaxReferencePhotos + 1)
            .Select(index => new AlbumPhoto { Id = $"r{index}", ContentType = "image/jpeg" })
            .ToList();
        Assert.NotNull(PhotoEditRules.ValidateReferences("a", many));
    }

    [Fact]
    public void TryReplaceSlot_only_changes_the_requested_slot_and_adds_photo_to_chapter()
    {
        var album = new Album { Chapters = [new AlbumChapter { Id = "ch", PhotoIds = ["orig"] }] };
        var layout = new ViewableAlbumLayout
        {
            Pages =
            [
                new LayoutPage
                {
                    Number = 2,
                    ChapterId = "ch",
                    Photos = [new PhotoSlot { PhotoId = "orig" }, new PhotoSlot { PhotoId = "orig" }]
                }
            ]
        };

        Assert.True(PhotoEditRules.TryReplaceSlot(album, layout, 2, 1, "orig", "edit"));

        Assert.Equal("orig", layout.Pages[0].Photos[0].PhotoId);
        Assert.Equal("edit", layout.Pages[0].Photos[1].PhotoId);
        Assert.Contains("edit", album.Chapters[0].PhotoIds);
    }

    [Fact]
    public void TryReplaceSlot_fails_when_slot_no_longer_holds_original()
    {
        var layout = new ViewableAlbumLayout
        {
            Pages = [new LayoutPage { Number = 1, Photos = [new PhotoSlot { PhotoId = "other" }] }]
        };

        Assert.False(PhotoEditRules.TryReplaceSlot(new Album(), layout, 1, 0, "orig", "edit"));
        Assert.False(PhotoEditRules.TryReplaceSlot(new Album(), layout, 1, 5, "orig", "edit"));
        Assert.Equal("other", layout.Pages[0].Photos[0].PhotoId);
    }
}
