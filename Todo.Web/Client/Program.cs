using System;
using System.Threading.Tasks;
using Ganss.Xss;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Todo.Web.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddHttpClient<TodoClient>(client =>
        {
            client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

            // The cookie auth stack detects this header and avoids redirects for unauthenticated
            // requests
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        });
        builder.Services.AddSingleton<HtmlSanitizer>();

        await builder.Build().RunAsync();
    }
}