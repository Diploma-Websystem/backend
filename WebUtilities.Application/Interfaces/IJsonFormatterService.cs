namespace WebUtilities.Application.Interfaces;

public interface IJsonFormatterService
{
    string Process(string json, bool minify);
}
