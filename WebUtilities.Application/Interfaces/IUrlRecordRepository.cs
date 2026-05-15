namespace WebUtilities.Application.Interfaces;

public interface IUrlRecordRepository
{
    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
    Task AddAsync(string originalUrl, string shortCode, string? userId, CancellationToken cancellationToken = default);
    Task<string?> GetOriginalUrlByCodeAsync(string shortCode, CancellationToken cancellationToken = default);
}
