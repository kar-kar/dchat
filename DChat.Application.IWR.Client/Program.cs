using DChat.Application.Shared.ClientServer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;

namespace DChat.Application.IWR.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
            builder.Services.AddBuildVersionCascadingValue();
            await LoadServerList(builder);

            await builder.Build().RunAsync();
        }

        private static async Task LoadServerList(WebAssemblyHostBuilder builder)
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
            var servers = await httpClient.GetFromJsonAsync<ServerInfo[]>("servers");
            builder.Services.AddCascadingValue(_ => servers);
        }
    }
}
