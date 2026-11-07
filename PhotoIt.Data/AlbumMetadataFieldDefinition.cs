namespace PhotoIt.Data;

public sealed record AlbumMetadataFieldDefinition(
    string Id,
    string Label,
    AlbumMetadataValueType ValueType = AlbumMetadataValueType.Text,
    string? Unit = null,
    bool ShowOnCover = false);
