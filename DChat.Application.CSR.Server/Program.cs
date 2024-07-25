using DChat.Application.CSR.Server.Components;
using DChat.Application.Shared.ClientServer.Components.Layout;
using DChat.Application.Shared.Server.Components.Account;
using DChat.Application.Shared.Server.Components.Account.Layout;
using DChat.Application.Shared.Server.Services;
using DChat.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;

namespace DChat.Application.CSR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();

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
                app.UseWebAssemblyDebugging();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly)
                .AddAdditionalAssemblies(typeof(AccountLayout).Assembly)
                .AddAdditionalAssemblies(typeof(MainLayout).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.MapHub<ChatSignalRHub>("/chathub")
                .WithHttpLogging(HttpLoggingFields.All);

            app.Run();
        }
    }
}
