using Microsoft.AspNetCore.Identity;

namespace DChat.Application.Shared.Server.Components.Account
{
    public static class IdentityExtensions
    {
        public static void ConfigureOptions(IdentityOptions options)
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 2;
            options.Password.RequiredUniqueChars = 1;
        }
    }
}
