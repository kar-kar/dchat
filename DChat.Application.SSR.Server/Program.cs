using DChat.Application.Shared.ClientServer.Components.Layout;
using DChat.Application.Shared.Server.Components.Account;
using DChat.Application.Shared.Server.Components.Account.Layout;
using DChat.Application.Shared.Server.Services;
using DChat.Application.SSR.Server.Components;
using DChat.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;

namespace DChat.Application.SSR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddRazorComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddIdentityCookies();

            builder.AddSqlServerDbContext<ChatDbContext>("chatdb");

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services
                .AddIdentityCore<ChatUser>(IdentityExtensions.ConfigureOptions)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ChatDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();
            
            builder.Services.AddScoped<IUserClaimsPrincipalFactory<ChatUser>, ChatUserClaimsPrincipalFactory>();

            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            builder.AddRabbitMQClient("rabbit");
            builder.Services.AddSingleton<NotificationsService>();
            builder.Services.AddScoped<ChatService>();
            builder.Services.AddHostedService<NotificationsProcessingService>();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddAdditionalAssemblies(typeof(MainLayout).Assembly)
                .AddAdditionalAssemblies(typeof(AccountLayout).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.MapHub<ChatSignalRHub>("/chathub")
                .WithHttpLogging(HttpLoggingFields.All);

            app.Run();
        }
    }
}
