using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace PhotoIt.Web.VisionEngine;

public sealed class VisionEngineClient
{
    private readonly HttpClient _http;
    private readonly VisionEngineOptions _options;
    private readonly TokenCredential _credential;

    public VisionEngineClient(HttpClient http, IOptions<VisionEngineOptions> options)
    {
        _http = http;
        _options = options.Value;
        _credential = CreateCredential(_options);
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<IReadOnlyList<BlobImageReference>> UploadInputsAsync(
        IEnumerable<(string FileName, string ContentType, byte[] Content)> files,
        CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        foreach (var (fileName, contentType, content) in files)
        {
            var part = new ByteArrayContent(content);
            part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(part, "files", fileName);
        }

        using var request = await CreateRequestAsync(HttpMethod.Post, "api/image-tasks/inputs", ct);
        request.Content = form;
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<BlobImageReference>>(VisionEngineJson.Options, ct)
            ?? throw new InvalidOperationException("VisionEngine returned no input references.");
    }

    public async Task<ImageTaskStatus> SubmitAsync(ImageTaskSubmission submission, CancellationToken ct)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/image-tasks", ct);
        request.Content = JsonContent.Create(submission, options: VisionEngineJson.Options);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ImageTaskStatus>(VisionEngineJson.Options, ct)
            ?? throw new InvalidOperationException("VisionEngine returned no task status.");
    }

    public async Task<ImageTaskStatus?> GetStatusAsync(string taskId, CancellationToken ct)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/image-tasks/{Uri.EscapeDataString(taskId)}", ct);
        using var response = await _http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ImageTaskStatus>(VisionEngineJson.Options, ct);
    }

    public async Task<(byte[] Content, string ContentType)> DownloadOutputAsync(string taskId, int outputIndex, CancellationToken ct)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/image-tasks/{Uri.EscapeDataString(taskId)}/outputs/{outputIndex}", ct);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return (
            await response.Content.ReadAsByteArrayAsync(ct),
            response.Content.Headers.ContentType?.MediaType ?? "image/png");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("The VisionEngine integration is not configured.");

        var token = await _credential.GetTokenAsync(new TokenRequestContext([_options.Scope!]), ct);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(
            $"VisionEngine request failed ({(int)response.StatusCode}): {(body.Length > 300 ? body[..300] : body)}");
    }

    private static TokenCredential CreateCredential(VisionEngineOptions options) =>
        !string.IsNullOrWhiteSpace(options.ClientSecret)
        && !string.IsNullOrWhiteSpace(options.TenantId)
        && !string.IsNullOrWhiteSpace(options.ClientId)
            ? new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret)
            : new DefaultAzureCredential();
}
