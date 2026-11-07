namespace PhotoIt.Data;

public sealed record AlbumTypeDefinition(
    string Id,
    string Name,
    string Icon,
    string Description,
    string PresentationHint,
    IReadOnlyList<AlbumMetadataFieldDefinition> AlbumFields,
    IReadOnlyList<AlbumMetadataFieldDefinition> ChapterFields);
