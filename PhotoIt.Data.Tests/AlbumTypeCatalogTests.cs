using PhotoIt.Data;

namespace PhotoIt.Data.Tests;

public sealed class AlbumTypeCatalogTests
{
    [Fact]
    public void AllTypes_HaveUniqueIdsAndAlbumAndChapterCapabilities()
    {
        Assert.Equal(17, AlbumTypeCatalog.All.Count);
        Assert.Equal(
            new[]
            {
                "travel", "wedding", "birthday-party", "baby-kids", "food-cooking", "family",
                "graduation", "sports", "pet", "nature-wildlife", "holiday", "corporate-events",
                "school-years", "retirement", "bucket-list", "home-renovation", "recipe-food-journal"
            },
            AlbumTypeCatalog.All.Select(type => type.Id));
        Assert.Equal(AlbumTypeCatalog.All.Count,
            AlbumTypeCatalog.All.Select(type => type.Id).Distinct(StringComparer.Ordinal).Count());

        Assert.All(AlbumTypeCatalog.All, type =>
        {
            Assert.False(string.IsNullOrWhiteSpace(type.Name));
            Assert.False(string.IsNullOrWhiteSpace(type.Icon));
            Assert.NotEmpty(type.AlbumFields);
            Assert.NotEmpty(type.ChapterFields);
            Assert.Contains(type.AlbumFields, field => field.ShowOnCover);
            Assert.Equal(type.AlbumFields.Count, type.AlbumFields.Select(field => field.Id).Distinct().Count());
            Assert.Equal(type.ChapterFields.Count, type.ChapterFields.Select(field => field.Id).Distinct().Count());
            Assert.All(type.AlbumFields.Concat(type.ChapterFields), field =>
            {
                Assert.False(string.IsNullOrWhiteSpace(field.Id));
                Assert.False(string.IsNullOrWhiteSpace(field.Label));
            });
        });
        Assert.Equal(
            Enum.GetValues<AlbumMetadataValueType>().Order(),
            AlbumTypeCatalog.All
                .SelectMany(type => type.AlbumFields.Concat(type.ChapterFields))
                .Select(field => field.ValueType)
                .Distinct()
                .Order());
    }

    [Fact]
    public void WeddingType_ProvidesTypedSummaryAndChapterCapabilities()
    {
        var wedding = AlbumTypeCatalog.Get("wedding");

        Assert.Contains(wedding.AlbumFields, field =>
            field.Id == "wedding-date"
            && field.ValueType == AlbumMetadataValueType.Date
            && field.ShowOnCover);
        Assert.Contains(wedding.AlbumFields, field =>
            field.Id == "guest-count"
            && field.ValueType == AlbumMetadataValueType.Integer
            && field.ShowOnCover);
        Assert.Contains(wedding.ChapterFields, field =>
            field.Id == "participants"
            && field.ValueType == AlbumMetadataValueType.Text);
        Assert.Contains("whitespace", wedding.PresentationHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MetadataValues_AreScopedByAlbumTypeAndUpdatedWithoutDroppingOtherFields()
    {
        var metadata = AlbumTypeCatalog.SetValue([], "wedding", "venue", "Garden");
        metadata = AlbumTypeCatalog.SetValue(metadata, "wedding", "guest-count", "120");

        Assert.Equal("Garden", AlbumTypeCatalog.GetValue(metadata, "wedding", "venue"));
        Assert.Null(AlbumTypeCatalog.GetValue(metadata, "birthday-party", "venue"));
        Assert.Equal("120", AlbumTypeCatalog.GetValue(metadata, "wedding", "guest-count"));

        var updated = AlbumTypeCatalog.SetValue(metadata, "wedding", "venue", "Hall");

        Assert.Equal("Hall", AlbumTypeCatalog.GetValue(updated, "wedding", "venue"));
        Assert.Equal("120", AlbumTypeCatalog.GetValue(updated, "wedding", "guest-count"));
        Assert.Equal(2, updated.Count);
        Assert.Equal(metadata.Single(item => item.FieldId == "wedding:venue").Id,
            updated.Single(item => item.FieldId == "wedding:venue").Id);
        Assert.All(updated, item => Assert.False(string.IsNullOrWhiteSpace(item.Id)));
    }

    [Theory]
    [InlineData("guest-count", "12", null)]
    [InlineData("guest-count", "twelve", "Guests must be a whole number.")]
    [InlineData("wedding-date", "2026-06-14", null)]
    [InlineData("wedding-date", "14/06/2026", "Wedding date must be a valid date.")]
    public void ValidateValues_ValidatesCapabilityFieldTypes(string fieldId, string value, string? expectedError)
    {
        var wedding = AlbumTypeCatalog.Get("wedding");
        var metadata = AlbumTypeCatalog.SetValue([], wedding.Id, fieldId, value);

        Assert.Equal(expectedError, AlbumTypeCatalog.ValidateValues(wedding.AlbumFields, metadata, wedding.Id));
    }

    [Fact]
    public void UnknownAlbumType_FallsBackToTravel()
    {
        Assert.Same(AlbumTypeCatalog.Default, AlbumTypeCatalog.Get("unknown"));
    }

    [Theory]
    [InlineData("travel", LayoutTemplateIds.TravelStyle)]
    [InlineData("wedding", LayoutTemplateIds.OneBig)]
    [InlineData("food-cooking", LayoutTemplateIds.OneBig)]
    public void GetDefaultPageTemplateId_UsesSupportedTypeDefaults(string albumType, string expectedTemplateId)
    {
        Assert.Equal(expectedTemplateId, AlbumTypeCatalog.GetDefaultPageTemplateId(albumType));
    }
}
