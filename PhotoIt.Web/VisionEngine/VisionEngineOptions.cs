namespace PhotoIt.Web.VisionEngine;

public sealed class VisionEngineOptions
{
    public const string SectionName = "VisionEngine";

    public string? BaseUrl { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>Scope for the VisionEngine API, e.g. api://{api-client-id}/.default.</summary>
    public string? Scope { get; set; }

    public int PollIntervalSeconds { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 300;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Scope);
}
