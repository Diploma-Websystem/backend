using WebUtilities.Application.Models;

namespace WebUtilities.Application.Interfaces;

public interface IIpGeolocationService
{
    Task<IpAnalysisResult> LookupAsync(string? ipAddress, CancellationToken cancellationToken = default);
}
