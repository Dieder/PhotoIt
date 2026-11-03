namespace PhotoIt.Data;

internal static class LayoutGraphUpdater
{
    internal static void CopyInto(ViewableAlbumLayout target, ViewableAlbumLayout source)
    {
        target.Title = source.Title;
        target.Subtitle = source.Subtitle;
        target.CoverPhotoId = source.CoverPhotoId;
        target.UpdatedUtc = source.UpdatedUtc;

        var unmatchedPages = target.Pages.ToList();
        foreach (var sourcePage in source.Pages)
        {
            var pageIndex = unmatchedPages.FindIndex(page =>
                page.Number == sourcePage.Number
                && string.Equals(page.ChapterId, sourcePage.ChapterId, StringComparison.OrdinalIgnoreCase));
            var targetPage = pageIndex >= 0
                ? unmatchedPages[pageIndex]
                : new LayoutPage();

            if (pageIndex >= 0)
                unmatchedPages.RemoveAt(pageIndex);
            else
                target.Pages.Add(targetPage);

            CopyPageInto(targetPage, sourcePage);
        }

        foreach (var unmatchedPage in unmatchedPages)
            target.Pages.Remove(unmatchedPage);
    }

    private static void CopyPageInto(LayoutPage target, LayoutPage source)
    {
        target.Number = source.Number;
        target.ChapterId = source.ChapterId;
        target.TemplateId = source.TemplateId;
        CopyAppearanceInto(target.Appearance, source.Appearance);

        var unmatchedSlots = target.Photos.ToList();
        foreach (var sourceSlot in source.Photos)
        {
            var slotIndex = unmatchedSlots.FindIndex(slot => slot.Col == sourceSlot.Col && slot.Row == sourceSlot.Row);
            var targetSlot = slotIndex >= 0
                ? unmatchedSlots[slotIndex]
                : CloneSlot(sourceSlot);

            if (slotIndex >= 0)
                unmatchedSlots.RemoveAt(slotIndex);
            else
                target.Photos.Add(targetSlot);

            CopySlotInto(targetSlot, sourceSlot);
        }

        foreach (var unmatchedSlot in unmatchedSlots)
            target.Photos.RemoveAll(slot => ReferenceEquals(slot, unmatchedSlot));
    }

    private static void CopyAppearanceInto(PageAppearance target, PageAppearance source)
    {
        target.Title = source.Title;
        target.FontFamily = source.FontFamily;
        target.TextColor = source.TextColor;
        target.FontSize = source.FontSize;
        target.TitleAlignment = source.TitleAlignment;
        target.BackgroundStyle = source.BackgroundStyle;
        target.BackgroundColor = source.BackgroundColor;
        target.GradientEndColor = source.GradientEndColor;
    }

    private static void CopySlotInto(PhotoSlot target, PhotoSlot source)
    {
        target.Col = source.Col;
        target.Row = source.Row;
        target.ColSpan = source.ColSpan;
        target.RowSpan = source.RowSpan;
        target.PhotoId = source.PhotoId;
        target.OffsetX = source.OffsetX;
        target.OffsetY = source.OffsetY;
        target.PositionX = source.PositionX;
        target.PositionY = source.PositionY;
        target.Zoom = source.Zoom;

        target.Type ??= new PhotoType();
        target.Type.Id = source.Type.Id;
        target.Type.Name = source.Type.Name;
        target.Type.WidthCm = source.Type.WidthCm;
        target.Type.HeightCm = source.Type.HeightCm;
        target.Type.AspectRatio = source.Type.AspectRatio;
        target.Type.ExampleSize = source.Type.ExampleSize;
    }

    private static PhotoSlot CloneSlot(PhotoSlot source) =>
        new(source.Col, source.Row, new PhotoType(), source.ColSpan, source.RowSpan, source.PhotoId)
        {
            OffsetX = source.OffsetX,
            OffsetY = source.OffsetY,
            PositionX = source.PositionX,
            PositionY = source.PositionY,
            Zoom = source.Zoom,
        };
}
