using FinanceManager.Server.Database;
using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.Tests.Util
{
    internal class MockFmDb : IDbContextFactory<FinanceManagerContext>
    {
        public FinanceManagerContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<FinanceManagerContext>()
                //.UseInMemoryDatabase($"InMemoryFmTestDb-{DateTime.Now.ToFileTimeUtc()}")
                .Options;

            return new FinanceManagerContext(options);
        }
    }

    internal class MockAuthDb : IDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                //.UseInMemoryDatabase($"InMemoryAuthTestDb-{DateTime.Now.ToFileTimeUtc()}")
                .Options;

            var ctx = new AuthDbContext(options);
            //ctx.Users.Add(new Database.Domain.Auth.AppUser() { UserName = "test",  })

            return ctx;
        }
    }
}
