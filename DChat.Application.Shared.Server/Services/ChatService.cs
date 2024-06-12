using DChat.Data;
using Microsoft.EntityFrameworkCore;

namespace DChat.Application.Shared.Server.Services
{
    public class ChatService(ChatDbContext db)
    {
        public async Task<MessageView> AddMessage(string senderId, string room, string text)
        {
            var user = await db.Users.FindAsync(senderId);

            var message = new Message
            {
                Sender = senderId,
                Room = room,
                Text = text,
                Html = MessageEncoder.ToHtml(text),
                Timestamp = DateTime.UtcNow
            };

            db.Messages.Add(message);
            await db.SaveChangesAsync();

            return new MessageView
            {
                Room = message.Room,
                Id = message.Id,
                SenderId = message.Sender,
                SenderDisplayName = user?.DisplayName ?? senderId,
                Html = message.Html,
                Timestamp = message.Timestamp.ToUnixTimeMilliseconds()
            };
        }

        public IAsyncEnumerable<MessageView> GetMessagesBeforeId(string room, long? id, int count)
        {
            return GetMessages(room)
                .Where(msg => !id.HasValue || msg.Id < id)
                .OrderByDescending(msg => msg.Id)
                .Take(count)
                .AsAsyncEnumerable();
        }

        public IAsyncEnumerable<MessageView> GetMessagesAfterId(string room, long id)
        {
            return GetMessages(room)
                .Where(msg => msg.Id > id)
                .OrderBy(msg => msg.Id)
                .AsAsyncEnumerable();
        }

        private IQueryable<MessageView> GetMessages(string room)
        {
            return from msg in db.Messages.AsNoTracking()
                   join user in db.Users on msg.Sender equals user.Id into users
                   from user in users.DefaultIfEmpty()
                   where msg.Room == room
                   select new MessageView
                   {
                       Room = msg.Room,
                       Id = msg.Id,
                       SenderId = msg.Sender,
                       SenderDisplayName = user.DisplayName ?? msg.Sender,
                       Html = msg.Html,
                       Timestamp = msg.Timestamp.ToUnixTimeMilliseconds()
                   };
        }
    }
}
