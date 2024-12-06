using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlazorFrontendNew.BlazorRedux;
using BlazorFrontendNew.Redux;
using BlazorFrontendNew.Store;

namespace BlazorFrontendNew.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //CreateHostBuilder(args).Build().Run();
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout=TimeSpan.FromSeconds(15) });

            builder.Services.AddReduxStore<AppState, IAction>(
                new AppState(), Reducers.RootReducer, options =>
                {
                    options.GetLocation = state => state.Location;
                });

            await builder.Build().RunAsync();
        }

        /*public static WebAssemblyHostBuilder CreateHostBuilder(string[] args) =>
            BlazorWebAssemblyHost.CreateDefaultBuilder()
                .UseBlazorStartup<Startup>();*/
    }
}
