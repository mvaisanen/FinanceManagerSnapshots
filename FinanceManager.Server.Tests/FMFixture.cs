using FinanceManager.Server.Database;
using FinanceManager.Server.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.Tests
{
    public class FmFixture : IDisposable
    {
        private const string ConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2Test;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";
        private const string AuthConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2Test.Auth;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";
        private static bool seedingDone = false;
        private static object initLock = new Object();
        private static bool initialised = false;
        private static object seedingLock = new Object();

        public FmFixture() 
        {
            lock (initLock)
            {
                if (!initialised)
                {
                    using (var ctx = CreateAuthTestContext())
                    {
                        ctx.Database.EnsureDeleted();
                        ctx.Database.EnsureCreated();
                    }

                    using (var ctx = CreateFmTestContext())
                    {
                        ctx.Database.EnsureDeleted();
                        ctx.Database.EnsureCreated();
                    }
                }

                initialised = true;
            }
        }

        public void EnsureSeeded(IServiceScope scope)
        {
            lock (seedingLock)
            {
                if (!seedingDone)
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<TestDatabaseSeeder>();
                    seeder.Seed().Wait();
                    seedingDone = true;
                }
            }
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public FinanceManagerContext CreateFmTestContext()
        {
            return new FinanceManagerContext(new DbContextOptionsBuilder<FinanceManagerContext>()
                .UseSqlServer(ConnectionString)
                .Options);
        }

        public AuthDbContext CreateAuthTestContext()
        {
            return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlServer(AuthConnectionString)
                .Options);
        }
    }
}
