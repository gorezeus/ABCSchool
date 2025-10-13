using System.Security.Claims;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Constants;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Contexts;

public class ApplicationDbSeeder
{
    private readonly IMultiTenantContextAccessor<ABCSchoolTenantInfo> _multiTenantContextAccessor;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _applicationDbContext;

    public ApplicationDbSeeder(IMultiTenantContextAccessor<ABCSchoolTenantInfo> multiTenantContextAccessor,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _roleManager = roleManager;
        _userManager = userManager;
        _applicationDbContext = applicationDbContext;
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_applicationDbContext.Database.GetMigrations().Any())
        {
            if ((await _applicationDbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                await _applicationDbContext.Database.MigrateAsync(cancellationToken);
            }

            if (await _applicationDbContext.Database.CanConnectAsync(cancellationToken))
            {
                await InitializeDefaultRolesAsync(cancellationToken);
                await InitializeAdminUserAsync();
            }
        }
    }

    private async Task InitializeDefaultRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in RoleConstant.DefaultRoles)
        {
            if (await _roleManager.Roles.SingleOrDefaultAsync(
                role => role.Name == roleName, cancellationToken) is not ApplicationRole incomingRole)
            {
                incomingRole = new ApplicationRole()
                {
                    Name = roleName,
                    Description = $"{roleName} Role"
                };
                await _roleManager.CreateAsync(incomingRole);
            }

            //Assign Permission
            if (roleName == RoleConstant.Basic)
            {
                await AssignPermissionToRole(SchoolPermissions.Basic, incomingRole, cancellationToken);
            }
            else if (roleName == RoleConstant.Admin)
            {
                await AssignPermissionToRole(SchoolPermissions.Admin, incomingRole, cancellationToken);

                if (_multiTenantContextAccessor.MultiTenantContext?.TenantInfo.Id == TenancyConstants.Root.Id)
                {
                    await AssignPermissionToRole(SchoolPermissions.Root, incomingRole, cancellationToken);
                }
            }
        }
    }

    private async Task AssignPermissionToRole(
        IReadOnlyList<SchoolPermission> rolePermissions,
        ApplicationRole role,
        CancellationToken cancellationToken)
    {
        var currentClaims = await _roleManager.GetClaimsAsync(role);

        foreach (var rolePermission in rolePermissions)
        {
            if (!currentClaims.Any(c => c.Type == ClaimConstant.Permission && c.Value == rolePermission.Name))
            {
                await _applicationDbContext.RoleClaims.AddAsync(new ApplicationRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = ClaimConstant.Permission,
                    ClaimValue = rolePermission.Name,
                    Description = rolePermission.Description,
                    Group = rolePermission.Group
                }, cancellationToken);

                await _applicationDbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task InitializeAdminUserAsync()
    {
        if (string.IsNullOrEmpty(_multiTenantContextAccessor.MultiTenantContext?.TenantInfo.Email)) return;

        if (await _userManager.Users
                .SingleOrDefaultAsync(u => u.Email == _multiTenantContextAccessor.MultiTenantContext.TenantInfo.Email)
                is not ApplicationUser incomingUser)
        {
            incomingUser = new ApplicationUser
            {
                FirstName = TenancyConstants.FirstName,
                LastName = TenancyConstants.LastName,
                Email = _multiTenantContextAccessor.MultiTenantContext.TenantInfo.Email,
                UserName = _multiTenantContextAccessor.MultiTenantContext.TenantInfo.Email,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var passwordHash = new PasswordHasher<ApplicationUser>();
            incomingUser.PasswordHash = passwordHash.HashPassword(incomingUser, TenancyConstants.DefaultPassword);

            await _userManager.CreateAsync(incomingUser);
        }

        if (!await _userManager.IsInRoleAsync(incomingUser , RoleConstant.Admin))
            await _userManager.AddToRoleAsync(incomingUser, RoleConstant.Admin);
    }
}
