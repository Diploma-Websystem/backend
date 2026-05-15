using System.Security.Cryptography;
using WebUtilities.Application.Interfaces;

namespace WebUtilities.Application.Services;

public class UrlShortenerService : IUrlShortenerService
{
    private const string CodeAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int DefaultCodeLength = 6;
    private readonly IUrlRecordRepository _urlRecordRepository;

    public UrlShortenerService(IUrlRecordRepository urlRecordRepository)
    {
        _urlRecordRepository = urlRecordRepository;
    }

    public async Task<string> CreateShortCodeAsync(string originalUrl, string? userId, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Original URL is not a valid absolute URL.", nameof(originalUrl));
        }

        var shortCode = await GenerateUniqueCodeAsync(cancellationToken);
        await _urlRecordRepository.AddAsync(originalUrl, shortCode, userId, cancellationToken);
        return shortCode;
    }

    public Task<string?> GetOriginalUrlByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult<string?>(null);
        }

        return _urlRecordRepository.GetOriginalUrlByCodeAsync(code, cancellationToken);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var shortCode = CreateCode(DefaultCodeLength);
            var exists = await _urlRecordRepository.ShortCodeExistsAsync(shortCode, cancellationToken);
            if (!exists)
            {
                return shortCode;
            }
        }

        throw new InvalidOperationException("Could not generate unique short code.");
    }

    private static string CreateCode(int length)
    {
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        }

        return new string(chars);
    }
}
