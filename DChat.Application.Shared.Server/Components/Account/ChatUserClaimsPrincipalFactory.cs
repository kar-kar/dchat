﻿using DChat.Application.Shared.ClientServer;
using DChat.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DChat.Application.Shared.Server.Components.Account
{
    public class ChatUserClaimsPrincipalFactory(
        UserManager<ChatUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : UserClaimsPrincipalFactory<ChatUser, IdentityRole>(userManager, roleManager, options)
    {
        public override async Task<ClaimsPrincipal> CreateAsync(ChatUser user)
        {
            var principal = await base.CreateAsync(user);
            var identity = principal.Identity as ClaimsIdentity;

            if (identity is not null && !string.IsNullOrWhiteSpace(user.DisplayName))
                identity.AddClaim(new Claim(ChatUserClaimTypes.DisplayName, user.DisplayName));

            return principal;
        }
    }
}
