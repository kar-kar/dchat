using DChat.Application.Shared.ClientServer;

namespace DChat.Application.Shared.Server.Services
{
    public interface IChatSignalRClient
    {
       Task ReceiveMessage(MessageView msg);
    }
}
