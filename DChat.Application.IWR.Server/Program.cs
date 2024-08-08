using DChat.Application.IWR.Server.Components;
using DChat.Application.Shared.ClientServer;
using DChat.Application.Shared.ClientServer.Components.Layout;
using DChat.Application.Shared.Server.Components.Account;
using DChat.Application.Shared.Server.Components.Account.Layout;
using DChat.Application.Shared.Server.Services;
using DChat.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DChat.Application.IWR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var dbConnectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string 'Default' not found.");
            var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ") ?? throw new InvalidOperationException("Connection string 'RabbitMQ' not found.");

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

            builder.Services
                .AddDbContext<ChatDbContext>(options => options.UseSqlServer(dbConnectionString))
                .AddDatabaseDeveloperPageExceptionFilter();

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

            builder.Services.Configure<NotificationsServiceOptions>(options => options.RabbitMqConnectionString = rabbitMqConnectionString);
            builder.Services.AddSingleton<NotificationsService>();
            builder.Services.AddScoped<ChatService>();
            builder.Services.AddHostedService<NotificationsProcessingService>();
            builder.Services.AddBuildVersionCascadingValue();

            var servers = builder.Configuration.GetSection("Servers").Get<ServerInfo[]>() ?? [];
            builder.Services.AddCascadingValue(_ => servers);

            var app = builder.Build();

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

            //add endpoint providing server list for the client
            app.MapGet("/servers", () => Results.Ok(servers));

            app.Run();
        }
    }
}
