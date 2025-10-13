using Finbuckle.MultiTenant;
using Infrastructure.Contexts;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructureService(this IServiceCollection service, IConfiguration configuration)
    {
        // Add infrastructure services here, e.g., database context, repositories, etc.
        service.AddDbContext<TenantDbContext>(option =>
            option.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")))
                    .AddMultiTenant<ABCSchoolTenantInfo>()
                    .WithHeaderStrategy(TenancyConstants.TenantIdName)
                    .WithClaimStrategy(TenancyConstants.TenantIdName)
                    .WithEFCoreStore<TenantDbContext, ABCSchoolTenantInfo>();

        service.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        service.AddTransient<ITenantDbSeeder, TenantDbSeeder>();
        service.AddTransient<ApplicationDbSeeder>();
        service.AddIdentityServices();
        return service;
    }
    internal static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        // Add identity services here, e.g., UserManager, RoleManager, etc.
        services.AddIdentity<ApplicationUser, ApplicationRole>(option =>
        {
            option.Password.RequireDigit = false;
            option.Password.RequireLowercase = false;
            option.Password.RequireUppercase = false;
            option.Password.RequireNonAlphanumeric = false;
            option.Password.RequiredLength = 6;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
    public static async Task AddDatabaseInitializeerAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var tenantDbSeeder = scope.ServiceProvider.GetRequiredService<ITenantDbSeeder>();
        await tenantDbSeeder.InitializeDatabaseAsync(cancellationToken);
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
    {
        // Use infrastructure middleware here, e.g., for multi-tenancy
        app.UseMultiTenant();

        return app;
    }
}
