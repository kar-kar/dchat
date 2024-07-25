using DChat.Application.ISR.Server.Components;
using DChat.Application.Shared.ClientServer.Components.Layout;
using DChat.Application.Shared.Server.Components.Account;
using DChat.Application.Shared.Server.Components.Account.Layout;
using DChat.Application.Shared.Server.Services;
using DChat.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;

namespace DChat.Application.ISR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            }).AddIdentityCookies();

            builder.AddSqlServerDbContext<ChatDbContext>("chatdb");

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services
                .AddIdentityCore<ChatUser>(IdentityExtensions.ConfigureOptions)
                .AddEntityFrameworkStores<ChatDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.AddRabbitMQClient("rabbit");
            builder.Services.AddSingleton<NotificationsService>();
            builder.Services.AddScoped<ChatService>();

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
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(typeof(MainLayout).Assembly)
                .AddAdditionalAssemblies(typeof(AccountLayout).Assembly);

            app.Run();
        }
    }
}
