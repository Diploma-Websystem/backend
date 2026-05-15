using Microsoft.EntityFrameworkCore;
using WebUtilities.Application.Interfaces;
using WebUtilities.Core.Entities;
using WebUtilities.Infrastructure.Data;

namespace WebUtilities.Infrastructure.Repositories;

public class UrlRecordRepository : IUrlRecordRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UrlRecordRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.UrlRecords.AnyAsync(x => x.ShortCode == shortCode, cancellationToken);
    }

    public async Task AddAsync(string originalUrl, string shortCode, string? userId, CancellationToken cancellationToken = default)
    {
        var record = new UrlRecord
        {
            Id = Guid.NewGuid(),
            OriginalUrl = originalUrl,
            ShortCode = shortCode,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _dbContext.UrlRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<string?> GetOriginalUrlByCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.UrlRecords
            .Where(x => x.ShortCode == shortCode)
            .Select(x => x.OriginalUrl)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
