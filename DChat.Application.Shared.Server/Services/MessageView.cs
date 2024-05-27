namespace DChat.Application.Shared.Server.Services
{
    public class MessageView
    {
        public required string Room { get; set; }
        public required long Id { get; set; }
        public required string SenderId { get; set; }
        public required string SenderDisplayName { get; set; }
        public required string Html { get; set; }
        public required long Timestamp { get; set; }
    }
}
