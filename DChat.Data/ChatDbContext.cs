using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DChat.Data
{
    public class ChatDbContext(DbContextOptions<ChatDbContext> options) : IdentityDbContext<ChatUser>(options)
    {
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var identitySchema = "Identity";
            builder.Entity<ChatUser>().ToTable("Users", identitySchema);
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", identitySchema);
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", identitySchema);
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", identitySchema);
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", identitySchema);
            builder.Entity<IdentityRole>().ToTable("Roles", identitySchema);
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", identitySchema);

            builder.Entity<Message>()
                .HasOne<ChatUser>()
                .WithMany()
                .HasForeignKey(m => m.Sender)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
