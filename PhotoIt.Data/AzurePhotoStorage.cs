using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace PhotoIt.Data;

public sealed record AzurePhotoStorageOptions(
    string AccountName,
    string ContainerName = "photos",
    string? AccountUri = null,
    string? AccountKey = null);

public sealed class AzurePhotoStorage
{
    private readonly BlobServiceClient _serviceClient;
    private readonly BlobContainerClient _container;
    private readonly bool _canGenerateAccountSas;

    public AzurePhotoStorage(AzurePhotoStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.AccountName))
            throw new InvalidOperationException("Azure storage account name is not configured.");

        var accountUri = new Uri(options.AccountUri ?? $"https://{options.AccountName}.blob.core.windows.net");
        if (!string.IsNullOrWhiteSpace(options.AccountKey))
        {
            _serviceClient = new BlobServiceClient(accountUri, new StorageSharedKeyCredential(options.AccountName, options.AccountKey));
            _canGenerateAccountSas = true;
        }
        else
        {
            _serviceClient = new BlobServiceClient(accountUri, new DefaultAzureCredential());
        }

        _container = _serviceClient.GetBlobContainerClient(options.ContainerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<IReadOnlyDictionary<string, Uri>> CreateUploadSasUrisAsync(
        IEnumerable<string> blobNames,
        DateTimeOffset expiresOn,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(blobNames);
        var names = blobNames.Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length == 0)
            return new Dictionary<string, Uri>(StringComparer.Ordinal);

        UserDelegationKey? delegationKey = null;
        if (!_canGenerateAccountSas)
        {
            var now = DateTimeOffset.UtcNow;
            delegationKey = (await _serviceClient.GetUserDelegationKeyAsync(
                now.AddMinutes(-5),
                expiresOn,
                ct)).Value;
        }

        var sasUris = new Dictionary<string, Uri>(names.Length, StringComparer.Ordinal);
        foreach (var blobName in names)
        {
            var blob = _container.GetBlobClient(blobName);
            var sasUri = delegationKey is null
                ? blob.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, expiresOn)
                : blob.GenerateUserDelegationSasUri(
                    BlobSasPermissions.Create | BlobSasPermissions.Write,
                    expiresOn,
                    delegationKey);
            sasUris.Add(blobName, sasUri);
        }

        return sasUris;
    }

    public async Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken ct = default,
        bool overwrite = false)
    {
        var blob = _container.GetBlobClient(blobName);
        if (overwrite)
        {
            await blob.UploadAsync(content, overwrite: true, cancellationToken: ct);
            await blob.SetHttpHeadersAsync(
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: ct);
            return;
        }

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, ct);
    }

    public Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        return _container.GetBlobClient(blobName)
            .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }

    public async Task<BlobProperties> GetPropertiesAsync(string blobName, CancellationToken ct = default)
    {
        var response = await _container.GetBlobClient(blobName).GetPropertiesAsync(cancellationToken: ct);
        return response.Value;
    }

    public async Task<(Stream Content, string ContentType)> DownloadAsync(
        string blobName,
        CancellationToken ct = default)
    {
        var response = await _container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: ct);
        return (response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
    }

    public Uri GetReadUri(string blobName)
    {
        return _container.GetBlobClient(blobName).Uri;
    }
}