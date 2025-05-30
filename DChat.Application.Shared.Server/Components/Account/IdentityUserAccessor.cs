using DChat.Data;
using Microsoft.AspNetCore.Identity;

namespace DChat.Application.Shared.Server.Components.Account
{
    public sealed class IdentityUserAccessor(UserManager<ChatUser> userManager, IdentityRedirectManager redirectManager)
    {
        public async Task<ChatUser> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null)
            {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            }

            return user;
        }
    }
}
