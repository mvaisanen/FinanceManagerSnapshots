using FinanceManager.Server.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.IntegrationTests
{
    public class FmWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private const string ConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2Test;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";
        private const string AuthConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2Test.Auth;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";

        public FmWebApplicationFactory()
        {

        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                var authDescriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
                services.Remove(authDescriptor);
                services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(AuthConnectionString));
                
                var dbDescriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<FinanceManagerContext>));
                services.Remove(dbDescriptor);
                services.AddDbContext<FinanceManagerContext>(options => options.UseSqlServer(ConnectionString));

                services.AddScoped<TestDatabaseSeeder>();              
            });

            builder.UseEnvironment("Development");
        }



    }
}
