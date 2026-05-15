namespace WebUtilities.Application.Models;

public class IpAnalysisResult
{
    public string Query { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? RegionName { get; set; }
    public string? City { get; set; }
    public string? Zip { get; set; }
    public string? Timezone { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Isp { get; set; }
    public string? Org { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
