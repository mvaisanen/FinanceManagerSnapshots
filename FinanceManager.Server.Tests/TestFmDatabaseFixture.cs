using Financemanager.Server.Database.Domain;
using FinanceManager.Server.Database;
using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.Tests
{
    public class TestFmDatabaseFixture
    {
        private const string ConnectionString = @"Server=.\SQLEXPRESS;Database=FinanceManagerV2UnitTest;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=true";
        private static readonly object _lock = new();
        private static bool _databaseInitialized;

        public TestFmDatabaseFixture() 
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    using (var ctx = CreateContext())
                    {
                        ctx.Database.EnsureDeleted();
                        ctx.Database.EnsureCreated();

                        //var port1 = new Financemanager.Server.Database.Domain.Portfolio("abc123");
                        //ctx.Portfolios.Add(port1);
                        //
                        //var stock = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
                        //ctx.Stocks.Add(stock);
                        //
                        //port1.AddStockPurchase(stock, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
                        //
                        //ctx.SaveChanges();
                    }

                    _databaseInitialized = true;
                }
            }
        }

        public FinanceManagerContext CreateContext()
            => new FinanceManagerContext(
                new DbContextOptionsBuilder<FinanceManagerContext>()
                    .UseSqlServer(ConnectionString)
                    .Options);

    }
}
