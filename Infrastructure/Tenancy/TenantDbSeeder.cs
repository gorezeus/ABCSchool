using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenancy;

public class TenantDbSeeder : ITenantDbSeeder
{
    private readonly TenantDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public TenantDbSeeder(TenantDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        await InitializeDatabaseWithTenantAsync(cancellationToken);
        
        foreach (var tenant in await _context.TenantInfo.ToListAsync(cancellationToken))
        {
            await InitializeApplicationDbForTenantAsync(tenant, cancellationToken);
        }
    }

    private async Task InitializeDatabaseWithTenantAsync(CancellationToken cancellationToken)
    {
        if (await _context.TenantInfo.FindAsync(TenancyConstants.Root.Id, cancellationToken) is null)
        {
            var rootTenant = new ABCSchoolTenantInfo
            {
                Id = TenancyConstants.Root.Id,
                Identifier = TenancyConstants.Root.Id,
                Name = TenancyConstants.Root.Name,
                Email = TenancyConstants.Root.Email,
                FirstName = TenancyConstants.FirstName,
                LastName = TenancyConstants.LastName,
                IsActive = true,
                ValidUpTo = DateTime.Now.AddYears(10)
            };

            await _context.TenantInfo.AddAsync(rootTenant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task InitializeApplicationDbForTenantAsync(ABCSchoolTenantInfo currentTenant, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        _serviceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<ABCSchoolTenantInfo>()
            {
                TenantInfo = currentTenant
            };

        await scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>()
            .InitializeDatabaseAsync(cancellationToken); 
        

    }
}
