using DChat.Application.Shared.Components.Account;
using DChat.Application.Shared.Components.Layout;
using DChat.Application.SSR.Components;
using DChat.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DChat.Application.SSR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var dbConnectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string 'Default' not found.");
            var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ") ?? throw new InvalidOperationException("Connection string 'RabbitMQ' not found.");

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

            builder.Services
                .AddDbContext<ChatDbContext>(options => options.UseSqlServer(dbConnectionString))
                .AddDatabaseDeveloperPageExceptionFilter();

            builder.Services
                .AddIdentityCore<ChatUser>(IdentityExtensions.ConfigureOptions)
                .AddEntityFrameworkStores<ChatDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ChatUser>, IdentityNoOpEmailSender>();

            var app = builder.Build();

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
                .AddAdditionalAssemblies(typeof(MainLayout).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }
    }
}
