using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebUtilities.Core.Entities;
using WebUtilities.Infrastructure.Data;

namespace WebUtilities.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        services.AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);
        services.AddAuthorization();

        return services;
    }
}
