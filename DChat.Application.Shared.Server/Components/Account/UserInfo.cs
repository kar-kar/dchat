namespace DChat.Application.Shared.Server.Components.Account
{
    // Add properties to this class and update the server and client AuthenticationStateProviders
    // to expose more information about the authenticated user to the client.
    public class UserInfo
    {
        public required string UserId { get; set; }
        public required string UserName { get; set; }
    }
}
