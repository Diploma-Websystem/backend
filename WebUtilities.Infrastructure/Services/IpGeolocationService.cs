using System.Net.Http.Json;
using WebUtilities.Application.Interfaces;
using WebUtilities.Application.Models;

namespace WebUtilities.Infrastructure.Services;

public class IpGeolocationService : IIpGeolocationService
{
    private readonly HttpClient _httpClient;

    public IpGeolocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IpAnalysisResult> LookupAsync(string? ipAddress, CancellationToken cancellationToken = default)
    {
        var targetIp = string.IsNullOrWhiteSpace(ipAddress) ? string.Empty : ipAddress.Trim();
        var endpoint = $"json/{Uri.EscapeDataString(targetIp)}";

        var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(endpoint, cancellationToken);
        if (response is null)
        {
            return new IpAnalysisResult
            {
                Query = targetIp,
                Success = false,
                ErrorMessage = "No response from geolocation provider."
            };
        }

        return new IpAnalysisResult
        {
            Query = response.Query ?? targetIp,
            Country = response.Country,
            RegionName = response.RegionName,
            City = response.City,
            Zip = response.Zip,
            Timezone = response.Timezone,
            Latitude = response.Lat,
            Longitude = response.Lon,
            Isp = response.Isp,
            Org = response.Org,
            Success = string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase),
            ErrorMessage = response.Message
        };
    }

    private sealed class IpApiResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? Query { get; set; }
        public string? Country { get; set; }
        public string? RegionName { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? Timezone { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lon { get; set; }
        public string? Isp { get; set; }
        public string? Org { get; set; }
    }
}
