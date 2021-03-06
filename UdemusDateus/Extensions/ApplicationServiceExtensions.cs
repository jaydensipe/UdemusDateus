using Microsoft.EntityFrameworkCore;
using UdemusDateus.Data;
using UdemusDateus.Helpers;
using UdemusDateus.Interfaces;
using UdemusDateus.Services;
using UdemusDateus.SignalR;

namespace UdemusDateus.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<PresenceTracker>();
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<LogUserActivity>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);

        services.AddDbContext<DataContext>(options => { options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")); });

        return services;
    }
}