using Finbuckle.MultiTenant;
using Infrastructure.Contexts;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Builder;
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

        return service;
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
    {
        // Use infrastructure middleware here, e.g., for multi-tenancy
        app.UseMultiTenant();

        return app;
    }
}
