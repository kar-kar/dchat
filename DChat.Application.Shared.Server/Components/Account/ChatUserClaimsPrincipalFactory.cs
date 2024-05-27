using DChat.Application.Shared.ClientServer;
using DChat.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DChat.Application.Shared.Server.Components.Account
{
    public class ChatUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ChatUser, IdentityRole>
    {
        public ChatUserClaimsPrincipalFactory(
            UserManager<ChatUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options)
        {
        }

        public override async Task<ClaimsPrincipal> CreateAsync(ChatUser user)
        {
            var principal = await base.CreateAsync(user);
            var identity = principal.Identity as ClaimsIdentity;

            if (identity is not null)
            {
                if (!string.IsNullOrWhiteSpace(user.DisplayName))
                    identity.AddClaim(new Claim(ChatUserClaimTypes.DisplayName, user.DisplayName));

                if (!string.IsNullOrWhiteSpace(user.DefaultRoom))
                    identity.AddClaim(new Claim(ChatUserClaimTypes.DefaultRoom, user.DefaultRoom));
            }

            return principal;
        }
    }
}
