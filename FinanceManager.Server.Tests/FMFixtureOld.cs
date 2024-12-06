using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.IntegrationTests
{
    //NOTE: NOT USED ATM, LEAVING CODE FOR POSSIBLE FUTURE USE/NEED
    public class FMFixtureOld : IDisposable
    {
        //IWebHost _webHost;
        WebApplication _app;
        
        public FMFixtureOld() 
        {
            var runTask = Task.Run(() =>
            {
                //_webHost = CreateWebHostBuilder(new string[] { }).Build();
                //_webHost.Run();
                var builder = WebApplication.CreateBuilder(new string[] { });
                var startup = new TestStartup(builder.Configuration);
                startup.ConfigureServices(builder.Services);
                _app = builder.Build();
                startup.Configure(_app, _app.Environment, _app.Lifetime);

                _app.Run($"http://+:6001/");
            });

            Task.Delay(2500).Wait();
            System.Diagnostics.Debug.WriteLine("FM app host running");
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<TestStartup>()
                .ConfigureLogging(logging =>
                {
                    //logging.ClearProviders();
                    //logging.SetMinimumLevel(LogLevel.Trace);
                    //logging.AddConsole();
                    //logging.AddDebug();
                    //logging.AddFilter(DbLoggerCategory.Database.Connection.Name, LogLevel.Warning);
                    //logging.AddFilter(DbLoggerCategory.Database.Transaction.Name, LogLevel.Warning);
                    //logging.AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Warning);
                    //logging.AddFilter(DbLoggerCategory.ChangeTracking.Name, LogLevel.Warning);
                })
                .UseNLog()
                .UseUrls($"http://+:6001/"); //Changed to port 6001; This format doesnt need the machine ip so works in production + dev environments


        public async void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("Stopping FM app webhost..");
            //await _webHost.StopAsync();
            await _app.StopAsync();
        }
    }



}
