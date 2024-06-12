﻿using DChat.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace DChat.Application.Shared.Server.Services
{
    [Authorize]
    public class ChatSignalRHub: Hub<IChatSignalRClient>
    {
        private readonly ChatService chatService;
        private readonly NotificationsService notificationsService;
        private readonly UserManager<ChatUser> userManager;

        public ChatSignalRHub(
            ChatService chatService,
            NotificationsService notificationsService,
            UserManager<ChatUser> userManager)
        {
            this.chatService = chatService;
            this.notificationsService = notificationsService;
            this.userManager = userManager;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public Task Subscribe(string room)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, room);
        }

        public Task Unsubscribe(string room)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
        }

        public async Task SetDefaultRoom(string room)
        {
            if (Context.User is null)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await userManager.GetUserAsync(Context.User) ?? throw new UnauthorizedAccessException("User is not authenticated.");
            if (user.DefaultRoom == room)
                return;

            user.DefaultRoom = room;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException("Failed to update user's default room.");
        }

        public async Task SendMessage(InputMessage input)
        {
            var sender = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(sender))
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (string.IsNullOrEmpty(input.Room))
                throw new ArgumentException("Room is required.", nameof(input));

            if (string.IsNullOrEmpty(input.Text))
                return;

            var msg = await chatService.AddMessage(sender, input.Room, input.Text);

            notificationsService.SendMessage(msg);
        }

        public async IAsyncEnumerable<MessageView> GetMessagesBeforeId(string room, int? id, int count, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var messages =  chatService.GetMessagesBeforeId(room, id, count).WithCancellation(cancellationToken);

            await foreach (var message in messages)
                yield return message;
        }

        public async IAsyncEnumerable<MessageView> GetMessagesAfterId(string room, int id, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var messages = chatService.GetMessagesAfterId(room, id).WithCancellation(cancellationToken);

            await foreach (var message in messages)
                yield return message;
        }
    }
}
