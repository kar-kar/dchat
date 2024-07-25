using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DChat.Application.Shared.ClientServer
{
    public static class BuildVersion
    {
        public static void AddBuildVersionCascadingValue(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var module = assembly.Modules.FirstOrDefault();
            var version = module?.ModuleVersionId.ToString("N") ?? "unknown";
            services.AddCascadingValue("BuildVersion", _ => version);
        }
    }
}
