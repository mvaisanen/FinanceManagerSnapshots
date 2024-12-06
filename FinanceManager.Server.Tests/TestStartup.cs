using FinanceManager.Server.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FinanceManager.Server.IntegrationTests
{
    //NOT ACTUALLY USED ATM
    public class TestStartup: Startup
    {
        private const string ConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2Test;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";
        private const string AuthConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2Test.Auth;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";

        public TestStartup(IConfiguration configuration): base(configuration)
        {

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            //These two are meant to replace the real registrations done in Server's Startup
            services.AddDbContext<AuthDbContext>(options =>
               options.UseSqlServer(AuthConnectionString));

            services.AddDbContext<FinanceManagerContext>(options =>
                options.UseSqlServer(ConnectionString));


        }

        protected override void UseDatabases()
        {
            var opts = new DbContextOptionsBuilder<AuthDbContext>();
            opts.UseSqlServer(AuthConnectionString);
            using (var client = new AuthDbContext(opts.Options))
            {
                client.Database.EnsureDeleted();
                client.Database.EnsureCreated();
            }

            var fmOpts = new DbContextOptionsBuilder<FinanceManagerContext>();
            fmOpts.EnableDetailedErrors(true);
            fmOpts.UseSqlServer(ConnectionString);
            using (var client = new FinanceManagerContext(fmOpts.Options))
            {
                client.Database.EnsureDeleted();
                client.Database.EnsureCreated();
            }
        }
    }
}
