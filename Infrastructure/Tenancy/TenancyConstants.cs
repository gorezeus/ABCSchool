using Microsoft.AspNetCore.Http;

namespace Infrastructure.Tenancy;

public class TenancyConstants
{
    public const string TenantIdName = "tenant";
    public const string DefaultPassword = "P@ssw0rd!";
    public const string FirstName = "Andri";
    public const string LastName = "Bratakusuma";


    //public const string Root = "root";
    public static class Root
    {
        public const string Id = "root";
        public const string Name = "Root";
        public const string Email = "admin.root@abcschool.com";
    }
}
