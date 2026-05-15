using System.Text.Json;
using WebUtilities.Application.Interfaces;

namespace WebUtilities.Application.Services;

public class JsonFormatterService : IJsonFormatterService
{
    public string Process(string json, bool minify)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON payload cannot be empty.", nameof(json));
        }

        using var document = JsonDocument.Parse(json);
        var options = new JsonSerializerOptions
        {
            WriteIndented = !minify
        };

        return JsonSerializer.Serialize(document.RootElement, options);
    }
}
