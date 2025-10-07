using Domain.Entities;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Contexts;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(
        IMultiTenantContextAccessor<ABCSchoolTenantInfo> contextAccessor,
        DbContextOptions<ApplicationDbContext> options) : base(contextAccessor, options)
    {
    }

    //DbSet<School> Schools { get; set; }
    public DbSet<School> Schools => Set<School>();
}
