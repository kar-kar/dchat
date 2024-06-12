using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DChat.Data
{
    [PrimaryKey(nameof(Room), nameof(Id))]
    public class Message
    {
        public required string Room { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public required string Sender { get; set; }

        public required string Text { get; set; }

        public required string Html { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
