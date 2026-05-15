using Microsoft.Extensions.DependencyInjection;
using WebUtilities.Application.Interfaces;
using WebUtilities.Application.Services;

namespace WebUtilities.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUrlShortenerService, UrlShortenerService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IJsonFormatterService, JsonFormatterService>();
        services.AddScoped<IIpAnalyzerService, IpAnalyzerService>();

        return services;
    }
}
