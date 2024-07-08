using DChat.Application.Shared.ClientServer;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;

namespace DChat.Application.Shared.Server.Services
{
    public class NotificationsProcessingService(NotificationsService notificationsService, IHubContext<ChatSignalRHub, IChatSignalRClient> hubContext) : BackgroundService
    {
        private readonly NotificationsService notificationsService = notificationsService;
        private readonly IHubContext<ChatSignalRHub, IChatSignalRClient> hubContext = hubContext;

        private readonly Channel<MessageView> messages = Channel.CreateBounded<MessageView>(
            new BoundedChannelOptions(1000)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            notificationsService.MessageReceived += NotificationsService_MessageReceived;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var msg = await messages.Reader.ReadAsync(stoppingToken);
                    await hubContext.Clients.Group(msg.Room).ReceiveMessage(msg);
                }
            }
            finally
            {
                notificationsService.MessageReceived -= NotificationsService_MessageReceived;

            }
        }

        private void NotificationsService_MessageReceived(object? sender, MessageView e)
        {
            messages.Writer.TryWrite(e);
        }
    }
}
