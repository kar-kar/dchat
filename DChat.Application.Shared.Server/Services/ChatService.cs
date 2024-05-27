using DChat.Data;
using Microsoft.EntityFrameworkCore;

namespace DChat.Application.Shared.Server.Services
{
    public class ChatService
    {
        private readonly ChatDbContext db;

        public ChatService(ChatDbContext db)
        {
            this.db = db;
        }

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
                Timestamp = message.Timestamp.Ticks
            };
        }

        public IAsyncEnumerable<MessageView> GetMessagesBeforeId(string room, long? id, int count)
        {
            var query = from msg in db.Messages.AsNoTracking()
                        join user in db.Users on msg.Sender equals user.Id into users
                        from user in users.DefaultIfEmpty()
                        where msg.Room == room && (!id.HasValue || msg.Id < id)
                        orderby msg.Id descending
                        select new MessageView
                        {
                            Room = msg.Room,
                            Id = msg.Id,
                            SenderId = msg.Sender,
                            SenderDisplayName = user.DisplayName ?? msg.Sender,
                            Html = msg.Html,
                            Timestamp = msg.Timestamp.Ticks
                        };

            return query.Take(count).AsAsyncEnumerable();
        }

        public IAsyncEnumerable<MessageView> GetMessagesAfterId(string room, long id)
        {
            var query = from msg in db.Messages.AsNoTracking()
                        join user in db.Users on msg.Sender equals user.Id into users
                        from user in users.DefaultIfEmpty()
                        where msg.Room == room && msg.Id > id
                        orderby msg.Id
                        select new MessageView
                        {
                            Room = msg.Room,
                            Id = msg.Id,
                            SenderId = msg.Sender,
                            SenderDisplayName = user.DisplayName ?? msg.Sender,
                            Html = msg.Html,
                            Timestamp = msg.Timestamp.Ticks
                        };

            return query.AsAsyncEnumerable();
        }
    }
}
