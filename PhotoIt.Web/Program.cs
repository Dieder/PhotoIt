using AiFoundryLocal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoIt.AiOrchestration;
using PhotoIt.Core.Services;
using PhotoIt.Data;
using PhotoIt.Web.Components;
using PhotoIt.Web.Components.Account;
using PhotoIt.Web.Data;
using Microsoft.Extensions.Http;
using ImageMagick;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration["CosmosdbConnection"]
    ?? throw new InvalidOperationException("Cosmos DB connection string 'CosmosdbConnection' not found. Configure Parameters:CosmosdbConnection in the AppHost user secrets.");

builder.Services.AddDbContextFactory<AlbumWebContext>(options =>
    options.UseCosmos(connectionString, "PhotoIt"));

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.Services.AddHttpContextAccessor();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddOutputCache();
builder.Services.AddSingleton<IPhotoItContextFactory, AlbumWebContextFactory>();
builder.Services.AddSingleton<AlbumCatalog>();
builder.Services.AddSingleton<PhotoTagCatalog>();
builder.Services.AddSingleton<PhotoThumbnailService>();

builder.Services.AddHttpClient<NominatimClient>();

builder.Services.AddSingleton(_ =>
{
    var accountUri = builder.Configuration["AzureStorage:AccountUri"];
    var accountName = builder.Configuration["AzureStorage:AccountName"];
    if (string.IsNullOrWhiteSpace(accountName) && Uri.TryCreate(accountUri, UriKind.Absolute, out var parsedUri))
        accountName = parsedUri.Host.Split('.')[0];
    if (string.IsNullOrWhiteSpace(accountName))
        throw new InvalidOperationException("Configure AzureStorage:AccountName or AzureStorage:AccountUri.");

    return new AzurePhotoStorage(new AzurePhotoStorageOptions(
        accountName,
        builder.Configuration["AzureStorage:ContainerName"] ?? "photos",
        accountUri,
        builder.Configuration["photostorageaccountkey"]));
});

builder.Services.AddSingleton(_ =>
{
    var configuredPath = builder.Configuration["VisionModel:ModelDirectory"];
    var modelDirectory = Path.IsPathRooted(configuredPath ?? string.Empty)
        ? configuredPath!
        : Path.GetFullPath(Path.Combine(
            builder.Environment.ContentRootPath,
            configuredPath ?? "..\\..\\models\\onnx_phi3_vision\\cpu_and_mobile\\cpu-int4-rtn-block-32-acc-level-4"));
    return new Phi3VisionModel(new Phi3VisionOptions(
        modelDirectory,
        builder.Configuration.GetValue("VisionModel:MaxNewTokens", 128),
        builder.Configuration.GetValue("VisionModel:ContextLength", 4096)));
});
builder.Services.AddSingleton<FileIndexer>(_ => new FileIndexer(
    builder.Configuration["VisionModel:LocalPhotoDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "photos")));
builder.Services.AddSingleton<PhotoVisionOrchestrator>();
builder.Services.AddSingleton<PhotoIndexQueue>();
builder.Services.AddHostedService<PhotoIndexBackgroundService>();
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId ?? string.Empty;
        googleOptions.ClientSecret = googleClientSecret ?? string.Empty;
    });

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<AlbumWebContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapGet("/albums/{albumId}/photos/{photoId}/content", async (
    string albumId,
    string photoId,
    AlbumCatalog catalog,
    AzurePhotoStorage storage,
    CancellationToken ct) =>
{
    var photo = await catalog.GetPhotoAsync(albumId, photoId, ct);
    if (photo is null)
        return Results.NotFound();

    var download = await storage.DownloadAsync(photo.BlobName, ct);
    return Results.Stream(download.Content, download.ContentType, enableRangeProcessing: true);
});

app.MapGet("/albums/{albumId}/photos/{photoId}/thumbnail", async (
    string albumId,
    string photoId,
    AlbumCatalog catalog,
    AzurePhotoStorage storage,
    PhotoThumbnailService thumbnails,
    ILogger<PhotoThumbnailService> logger,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    httpContext.Response.Headers.CacheControl = "private, max-age=86400";
    var photo = await catalog.GetPhotoAsync(albumId, photoId, ct);
    if (photo is null)
        return Results.NotFound();

    if (photo.ThumbnailUnavailable)
        return Results.Redirect($"/albums/{albumId}/photos/{photoId}/content");

    if (!string.IsNullOrWhiteSpace(photo.ThumbnailBlobName))
    {
        var storedThumbnail = await storage.DownloadAsync(photo.ThumbnailBlobName, ct);
        return Results.File(storedThumbnail.Content, storedThumbnail.ContentType, enableRangeProcessing: true);
    }

    var original = await storage.DownloadAsync(photo.BlobName, ct);
    await using var originalContent = original.Content;
    using var source = new MemoryStream();
    await originalContent.CopyToAsync(source, ct);

    byte[] thumbnail;
    try
    {
        thumbnail = thumbnails.Create(source.ToArray());
    }
    catch (MagickException exception)
    {
        photo.ThumbnailUnavailable = true;
        await catalog.UpdatePhotoAsync(photo, ct);
        logger.LogWarning(exception, "Could not create photo thumbnail. AlbumId: {AlbumId}, MediaId: {MediaId}", albumId, photoId);
        return Results.Redirect($"/albums/{albumId}/photos/{photoId}/content");
    }

    var separator = photo.BlobName.LastIndexOf('/');
    var photoFolder = separator > 0 ? photo.BlobName[..separator] : albumId;
    var thumbnailBlobName = $"{photoFolder}/thumbnails/{photoId}.jpg";
    await storage.UploadAsync(
        thumbnailBlobName,
        new MemoryStream(thumbnail),
        "image/jpeg",
        ct,
        overwrite: true);
    photo.ThumbnailBlobName = thumbnailBlobName;
    await catalog.UpdatePhotoAsync(photo, ct);

    return Results.File(new MemoryStream(thumbnail), "image/jpeg", enableRangeProcessing: true);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapAdditionalIdentityEndpoints();;

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AlbumWebContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
