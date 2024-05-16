using Microsoft.AspNetCore.Identity;

namespace DChat.Data
{
    public class ChatUser: IdentityUser
    {
        public string? DisplayName { get; set; }
    }
}
