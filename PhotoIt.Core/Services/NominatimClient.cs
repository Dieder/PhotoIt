using System.Globalization;
using System.Net.Http.Json;

namespace PhotoIt.Core.Services
{
    public class NominatimClient
    {
        private static readonly SemaphoreSlim RequestGate = new(1, 1);
        private static DateTime nextRequestUtc;
        private readonly HttpClient _http;

        public NominatimClient(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("PhotoIt/1.0 (dieder.timmermaan@gmail.com)");
        }

        public async Task<GeoPoint?> ResolvePlaceAsync(string query, CancellationToken ct = default)
        {
            return (await SearchPlacesAsync(query, limit: 1, ct)).FirstOrDefault();
        }

        public async Task<IReadOnlyList<GeoPoint>> SearchPlacesAsync(
            string query,
            int limit = 5,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var url = $"search?q={Uri.EscapeDataString(query)}&format=jsonv2"
                + $"&limit={Math.Clamp(limit, 1, 10)}&addressdetails=1";
            var results = await GetFromNominatimAsync<List<NominatimResult>>(url, ct) ?? [];

            return results
                .Select(ToGeoPoint)
                .Where(point => point is not null)
                .Cast<GeoPoint>()
                .ToList();
        }

        public async Task<GeoPoint?> ReverseResolvePlaceAsync(
            double latitude,
            double longitude,
            CancellationToken ct = default)
        {
            if (!double.IsFinite(latitude) || latitude is < -90 or > 90
                || !double.IsFinite(longitude) || longitude is < -180 or > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), "Coordinates are outside the valid range.");
            }

            var url = string.Create(CultureInfo.InvariantCulture,
                $"reverse?format=jsonv2&lat={latitude}&lon={longitude}&zoom=10&addressdetails=1");
            var result = await GetFromNominatimAsync<NominatimResult>(url, ct);
            return result is null ? null : ToGeoPoint(result);
        }

        private async Task<T?> GetFromNominatimAsync<T>(string url, CancellationToken ct)
        {
            await RequestGate.WaitAsync(ct);
            try
            {
                var delay = nextRequestUtc - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, ct);

                nextRequestUtc = DateTime.UtcNow.AddSeconds(1);
                return await _http.GetFromJsonAsync<T>(url, ct);
            }
            finally
            {
                RequestGate.Release();
            }
        }

        private static GeoPoint? ToGeoPoint(NominatimResult result)
        {
            if (!double.TryParse(result.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !double.TryParse(result.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                return null;
            }

            return new GeoPoint
            {
                Id = result.DisplayName,
                Name = string.IsNullOrWhiteSpace(result.Name) ? result.DisplayName : result.Name,
                Country = result.Address?.Country,
                Latitude = latitude,
                Longitude = longitude
            };
        }
    }
}
