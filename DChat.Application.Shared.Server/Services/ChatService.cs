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

        public async Task<MessageView> AddMessage(string sender, string room, string text)
        {
            var message = new Message
            {
                Sender = sender,
                Room = room,
                Text = text,
                Html = MessageEncoder.ToHtml(text),
                Timestamp = DateTime.UtcNow
            };

            db.Messages.Add(message);
            await db.SaveChangesAsync();

            return MessageView.Create(message);
        }

        public IAsyncEnumerable<MessageView> GetMessagesBeforeId(string room, long? id, int count)
        {
            var query = db.Messages
                .Where(m => m.Room == room && (id == null || m.Id < id.Value))
                .OrderByDescending(m => m.Id)
                .Select(MessageView.FromMessageExpr)
                .Take(count);

            return query.AsAsyncEnumerable();
        }

        public IAsyncEnumerable<MessageView> GetMessagesAfterId(string room, long id, int count)
        {
            var query = db.Messages
                .Where(m => m.Room == room && m.Id > id)
                .OrderBy(m => m.Id)
                .Select(MessageView.FromMessageExpr)
                .Take(count);

            return query.AsAsyncEnumerable();
        }
    }
}
