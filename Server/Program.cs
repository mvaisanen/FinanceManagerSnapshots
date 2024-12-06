//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using NLog.Web;
//using Microsoft.AspNetCore.Builder;
//using Server;
//using FinanceManager.Server.Database.Domain.Auth;
//using FinanceManager.Server.Database;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using Server.Actors;
//using Server.Auth.Entities;
//using Server.Auth.Services;
//using Server.Database;
//using Server.Services;
//using System.Net.Sockets;
//using System.Net;
//using System.Text;
//using Microsoft.AspNetCore.Authorization;
//using Akka.Actor;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
//using Microsoft.Extensions.Hosting;

using Microsoft.AspNetCore.Builder;
using Server;
using System;

var logger = NLog.Web.NLogBuilder.ConfigureNLog("Nlog.config").GetCurrentClassLogger();
//var ConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";
//var AuthConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2.Auth;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";
try
{
    logger.Debug("Building webapp...");

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ApplicationName = typeof(Program).Assembly.FullName,
    });
    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);
    var app = builder.Build();
    startup.Configure(app, app.Environment, app.Lifetime);
    app.Run($"http://+:6001/");   
}
catch (Exception ex)
{
    //NLog: catch setup errors
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}

public partial class Program { }


