using System.Diagnostics.CodeAnalysis;

namespace DChat.Application.Shared.ClientServer
{
    public static class Check
    {
        public static void NotNull<T>([NotNull] T? value, string? message = null)
        {
            if (value is null)
                throw new InvalidOperationException(message);
        }
    }
}
