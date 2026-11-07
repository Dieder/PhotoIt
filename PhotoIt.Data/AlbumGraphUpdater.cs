namespace PhotoIt.Data;

internal static class AlbumGraphUpdater
{
    internal static void CopyInto(Album target, Album source)
    {
        target.CreatedUtc = source.CreatedUtc;
        target.UpdatedUtc = source.UpdatedUtc;
        target.ImageFolderName = source.ImageFolderName;
        target.Name = source.Name;
        target.FullDescription = source.FullDescription;
        target.AlbumType = source.AlbumType;
        target.TravelStartDate = source.TravelStartDate;
        target.TravelEndDate = source.TravelEndDate;
        target.ShowRouteOnCover = source.ShowRouteOnCover;
        target.UseChapterTitles = source.UseChapterTitles;
        target.DefaultChapterMode = source.DefaultChapterMode;
        target.Intro = source.Intro;
        target.ShareSlug = source.ShareSlug;
        target.ShareSeed = source.ShareSeed;

        CopyBackgroundFill(target.BackGroundFill, source.BackGroundFill);
        CopyTripGraph(target.TripGraph, source.TripGraph);
        CopyMetadataValues(target.Metadata, source.Metadata);
        CopyChapters(target.Chapters, source.Chapters);
    }

    private static void CopyBackgroundFill(BackGroundFill target, BackGroundFill source)
    {
        target.BackroundColor = source.BackroundColor;
        target.ImagePath = source.ImagePath;
    }

    private static void CopyTripGraph(TripGraph target, TripGraph source)
    {
        target.RouteDescription = source.RouteDescription;
        target.CreatedUtc = source.CreatedUtc;
        target.UpdatedUtc = source.UpdatedUtc;

        var unmatchedLegs = target.Legs.ToList();
        foreach (var sourceLeg in source.Legs)
        {
            var legIndex = unmatchedLegs.FindIndex(leg =>
                string.Equals(leg.Id, sourceLeg.Id, StringComparison.Ordinal));
            var targetLeg = legIndex >= 0 ? unmatchedLegs[legIndex] : CloneLeg(sourceLeg);

            if (legIndex >= 0)
                unmatchedLegs.RemoveAt(legIndex);
            else
                target.Legs.Add(targetLeg);

            CopyLegInto(targetLeg, sourceLeg);
        }

        foreach (var unmatchedLeg in unmatchedLegs)
            target.Legs.Remove(unmatchedLeg);
    }

    private static void CopyLegInto(RouteLeg target, RouteLeg source)
    {
        target.Transport = source.Transport;
        target.DistanceKm = source.DistanceKm;
        target.StartDate = source.StartDate;
        target.EndDate = source.EndDate;
        target.IsEditing = source.IsEditing;
        CopyRoutePoint(target.From, source.From);
        CopyRoutePoint(target.To, source.To);
    }

    private static void CopyRoutePoint(RoutePoint target, RoutePoint source)
    {
        target.Id = source.Id;
        target.FoundName = source.FoundName;
        target.Country = source.Country;
        target.Place = source.Place;
        target.Latitude = source.Latitude;
        target.Longitude = source.Longitude;
        target.Svg = source.Svg;
    }

    private static void CopyChapters(List<AlbumChapter> target, List<AlbumChapter> source)
    {
        var unmatchedChapters = target.ToList();
        foreach (var sourceChapter in source)
        {
            var chapterIndex = unmatchedChapters.FindIndex(chapter =>
                string.Equals(chapter.Id, sourceChapter.Id, StringComparison.Ordinal));
            var targetChapter = chapterIndex >= 0 ? unmatchedChapters[chapterIndex] : CloneChapter(sourceChapter);

            if (chapterIndex >= 0)
                unmatchedChapters.RemoveAt(chapterIndex);
            else
                target.Add(targetChapter);

            CopyChapterInto(targetChapter, sourceChapter);
        }

        foreach (var unmatchedChapter in unmatchedChapters)
            target.Remove(unmatchedChapter);
    }

    private static void CopyChapterInto(AlbumChapter target, AlbumChapter source)
    {
        target.Title = source.Title;
        target.StartDate = source.StartDate;
        target.EndDate = source.EndDate;
        target.ThumbnailPhotoId = source.ThumbnailPhotoId;
        target.Mode = source.Mode;
        target.PhotoIds = source.PhotoIds.ToList();
        CopyMetadataValues(target.Metadata, source.Metadata);
    }

    private static void CopyMetadataValues(List<AlbumMetadataValue> target, List<AlbumMetadataValue> source)
    {
        var unmatchedValues = target.ToList();
        foreach (var sourceValue in source)
        {
            var valueIndex = unmatchedValues.FindIndex(value =>
                string.Equals(value.Id, sourceValue.Id, StringComparison.Ordinal));
            var targetValue = valueIndex >= 0 ? unmatchedValues[valueIndex] : CloneMetadataValue(sourceValue);

            if (valueIndex >= 0)
                unmatchedValues.RemoveAt(valueIndex);
            else
                target.Add(targetValue);

            CopyMetadataValueInto(targetValue, sourceValue);
        }

        foreach (var unmatchedValue in unmatchedValues)
            target.Remove(unmatchedValue);
    }

    private static void CopyMetadataValueInto(AlbumMetadataValue target, AlbumMetadataValue source)
    {
        target.FunctionalName = source.FunctionalName;
        target.FieldId = source.FieldId;
        target.Value = source.Value;
    }

    private static AlbumMetadataValue CloneMetadataValue(AlbumMetadataValue source) => new()
    {
        Id = source.Id,
        FunctionalName = source.FunctionalName,
        FieldId = source.FieldId,
        Value = source.Value,
    };

    private static AlbumChapter CloneChapter(AlbumChapter source) => new()
    {
        Id = source.Id,
    };

    private static RouteLeg CloneLeg(RouteLeg source) => new()
    {
        Id = source.Id,
    };
}
