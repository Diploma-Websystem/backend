using WebUtilities.Application.Models;
using WebUtilities.Application.Interfaces;

namespace WebUtilities.Application.Services;

public class IpAnalyzerService : IIpAnalyzerService
{
    private readonly IIpGeolocationService _ipGeolocationService;

    public IpAnalyzerService(IIpGeolocationService ipGeolocationService)
    {
        _ipGeolocationService = ipGeolocationService;
    }

    public Task<IpAnalysisResult> AnalyzeAsync(string? ipAddress, CancellationToken cancellationToken = default)
    {
        return _ipGeolocationService.LookupAsync(ipAddress, cancellationToken);
    }
}
