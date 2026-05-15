namespace WebUtilities.Application.Interfaces;

public interface IUrlShortenerService
{
    Task<string> CreateShortCodeAsync(string originalUrl, string? userId, CancellationToken cancellationToken = default);
    Task<string?> GetOriginalUrlByCodeAsync(string code, CancellationToken cancellationToken = default);
}
