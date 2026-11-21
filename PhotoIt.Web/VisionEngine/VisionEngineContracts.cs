using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhotoIt.Web.VisionEngine;

// Wire-format mirror of the VisionEngine API contracts.
public enum ImageTaskOperation
{
    Generate,
    ReplaceObject,
    RemoveObject,
    RemoveBackground,
    ChangeBackground,
    ExpandCanvas,
    ImproveQuality,
    ChangeStyle,
    ProductCatalog,
    MarketingBanner,
    CharacterConsistency,
    SocialMediaReformat,
    ReplaceText,
    MultiReferenceComposition,
    Free,
    InPainting,
    CorrectColors
}

public enum ImageTaskState
{
    Queued,
    Processing,
    Processed,
    Poisoned,
    SubmissionFailed
}

public sealed record BlobImageReference(string ContainerName, string BlobName, string ContentType = "image/png");

public sealed record ImageTaskSubmission
{
    public required ImageTaskOperation Operation { get; init; }
    public required string Prompt { get; init; }
    public string? Model { get; init; }
    public IReadOnlyList<BlobImageReference> Inputs { get; init; } = [];
    public int Width { get; init; } = 1024;
    public int Height { get; init; } = 1024;
    public int Count { get; init; } = 1;
}

public sealed record ImageTaskStatus(
    string TaskId,
    ImageTaskOperation Operation,
    ImageTaskState State,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<BlobImageReference> Outputs,
    string? FailureReason = null,
    long? AttemptCount = null);

public static class VisionEngineJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed record PhotoEditOperation(ImageTaskOperation Operation, string Name, string Description, bool NeedsReferences = false);

public static class PhotoEditOperations
{
    public static IReadOnlyList<PhotoEditOperation> All { get; } =
    [
        new(ImageTaskOperation.ImproveQuality, "Improve quality", "Sharpens, denoises and enhances detail without changing the content."),
        new(ImageTaskOperation.CorrectColors, "Correct colors", "Fixes white balance, exposure and color cast for natural-looking colors."),
        new(ImageTaskOperation.RemoveObject, "Remove object", "Removes the object or person described in the prompt and fills the gap naturally."),
        new(ImageTaskOperation.ReplaceObject, "Replace object", "Replaces the object described in the prompt with something else. Describe both the object and the replacement."),
        new(ImageTaskOperation.RemoveBackground, "Remove background", "Cuts out the main subject and removes the background."),
        new(ImageTaskOperation.ChangeBackground, "Change background", "Keeps the subject and replaces the background with the scene you describe."),
        new(ImageTaskOperation.ChangeStyle, "Change style", "Redraws the photo in another style, for example watercolor, pencil sketch or vintage."),
        new(ImageTaskOperation.ExpandCanvas, "Expand canvas", "Extends the photo beyond its borders and generates the missing surroundings."),
        new(ImageTaskOperation.InPainting, "In-painting", "Regenerates the part of the photo you describe while leaving the rest untouched."),
        new(ImageTaskOperation.Free, "Free edit", "Follows your prompt freely, with no predefined behavior."),
        new(ImageTaskOperation.MultiReferenceComposition, "Compose with references", "Combines the photo with the selected reference photos into one composition.", NeedsReferences: true),
        new(ImageTaskOperation.CharacterConsistency, "Keep character consistent", "Places the person or character from the reference photos into the scene you describe.", NeedsReferences: true),
    ];

    public static PhotoEditOperation? Find(ImageTaskOperation operation) =>
        All.FirstOrDefault(candidate => candidate.Operation == operation);
}
