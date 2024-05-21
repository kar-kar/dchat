using DChat.Data;
using System.Linq.Expressions;

namespace DChat.Services
{
    public class MessageView
    {
        public required string Room { get; set; }
        public required long Id { get; set; }
        public required string Sender { get; set; }
        public required string Html { get; set; }
        public required long Timestamp { get; set; }

        public static Expression<Func<Message, MessageView>> FromMessageExpr => m => new MessageView
        {
            Room = m.Room,
            Id = m.Id,
            Sender = m.Sender,
            Html = m.Html,
            Timestamp = m.Timestamp.Ticks
        };

        public static Func<Message, MessageView> Create { get; } = FromMessageExpr.Compile();
    }
}
