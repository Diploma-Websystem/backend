using WebUtilities.Application.Models;

namespace WebUtilities.Application.Interfaces;

public interface IIpAnalyzerService
{
    Task<IpAnalysisResult> AnalyzeAsync(string? ipAddress, CancellationToken cancellationToken = default);
}
