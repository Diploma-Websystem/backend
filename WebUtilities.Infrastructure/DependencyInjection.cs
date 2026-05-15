using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebUtilities.Application.Interfaces;
using WebUtilities.Core.Entities;
using WebUtilities.Infrastructure.Data;
using WebUtilities.Infrastructure.Identity;
using WebUtilities.Infrastructure.Repositories;
using WebUtilities.Infrastructure.Services;

namespace WebUtilities.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        services.AddSingleton<IEmailSender<ApplicationUser>, ConsoleEmailSender>();
        services.AddScoped<IUrlRecordRepository, UrlRecordRepository>();
        services.AddHttpClient<IIpGeolocationService, IpGeolocationService>(client =>
        {
            client.BaseAddress = new Uri("http://ip-api.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
