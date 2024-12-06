using Financemanager.Server.Database.Domain;
using FinanceManager.Server.Database;
using FinanceManager.Server.IntegrationTests;
using FinanceManager.Server.Tests.Util;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Controllers;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.Tests.UnitTests
{
    public class PortfolioModelUnitTests: IClassFixture<TestFmDatabaseFixture>
    {
        public TestFmDatabaseFixture Fixture { get; }
        public PortfolioModelUnitTests(TestFmDatabaseFixture fixture)
        { 
            Fixture = fixture;
        }

        [Fact]
        public async Task Creating_portfolio_and_purchase_via_portfolio_model_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    await SeedPortfolioAndStock(ctx, "abc123");

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    Assert.NotNull(portFromDb);
                    Assert.True(portFromDb.Positions.Count == 1);
                    Assert.True(portFromDb.Positions.ElementAt(0).Stock.Ticker == "XYZ");
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.Count == 1);
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.ElementAt(0).Price == 124);
                }
            }
        }

        [Fact]
        public async Task Adding_two_purchases_to_same_position_via_portfolio_model_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var port1 = new Portfolio("abc123");
                    ctx.Portfolios.Add(port1);

                    var stock = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
                    ctx.Stocks.Add(stock);

                    port1.AddStockPurchase(stock, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
                    port1.AddStockPurchase(stock, DateTime.UtcNow.AddDays(-1), 33, 114, Common.Broker.InteractiveBrokers);
                    await ctx.SaveChangesAsync();

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    Assert.NotNull(portFromDb);
                    Assert.True(portFromDb.Positions.Count == 1);
                    Assert.True(portFromDb.Positions.ElementAt(0).Stock.Ticker == "XYZ");
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.Count == 2);
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.ElementAt(0).Price == 124);
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.ElementAt(1).Price == 114);
                }
            }
        }

        [Fact]
        public async Task Adding_two_purchases_to_different_positions_via_portfolio_model_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var port1 = new Portfolio("abc123");
                    ctx.Portfolios.Add(port1);

                    var stock1 = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
                    var stock2 = new Stock("ABC", "ABC Company", 456, 9.6, 15, 31.8, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector 2");
                    ctx.Stocks.Add(stock1);
                    ctx.Stocks.Add(stock2);
                    await ctx.SaveChangesAsync();

                    port1.AddStockPurchase(stock1, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
                    port1.AddStockPurchase(stock2, DateTime.UtcNow.AddDays(-1), 55, 134, Common.Broker.InteractiveBrokers);
                    await ctx.SaveChangesAsync();

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    Assert.NotNull(portFromDb);
                    Assert.True(portFromDb.Positions.Count == 2);
                    Assert.True(portFromDb.Positions.ElementAt(0).Stock.Ticker == "XYZ");
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.Count == 1);
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.ElementAt(0).Price == 124);
                    Assert.True(portFromDb.Positions.ElementAt(1).Stock.Ticker == "ABC");
                    Assert.True(portFromDb.Positions.ElementAt(1).Buys.Count == 1);
                    Assert.True(portFromDb.Positions.ElementAt(1).Buys.ElementAt(0).Price == 134);
                }
            }
        }

        [Fact]
        public async Task Removing_only_purchase_from_position_should_delete_position_itself()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    await SeedPortfolioAndStock(ctx, "abc123");

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    var purchaseIdToDel = portFromDb.Positions.ElementAt(0).Buys.ElementAt(0).StockPurchaseId;
                    portFromDb.RemoveStockPurchase(purchaseIdToDel);
                    await ctx.SaveChangesAsync();

                    portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");

                    Assert.NotNull(portFromDb);
                    Assert.True(portFromDb.Positions.Count == 0);
                    var purchasesRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM StockPurchase");
                    Assert.True(purchasesRemaining.ToList()[0] == 0);
                }
            }
        }

        [Fact]
        public async Task Removing_purchase_from_position_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var port1 = new Portfolio("abc123");
                    ctx.Portfolios.Add(port1);

                    var stock = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
                    ctx.Stocks.Add(stock);
                    await ctx.SaveChangesAsync();

                    port1.AddStockPurchase(stock, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
                    port1.AddStockPurchase(stock, DateTime.UtcNow.AddDays(-1), 44, 121, Common.Broker.InteractiveBrokers);
                    await ctx.SaveChangesAsync();

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    var purchaseIdToDel = portFromDb.Positions.ElementAt(0).Buys.ElementAt(0).StockPurchaseId; //Deletes the purchase of 42 stocks, the 44 buy remains
                    portFromDb.RemoveStockPurchase(purchaseIdToDel);
                    await ctx.SaveChangesAsync();

                    portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");

                    Assert.NotNull(portFromDb);
                    Assert.True(portFromDb.Positions.Count == 1);
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.Count == 1);
                    Assert.True(portFromDb.Positions.ElementAt(0).Buys.ElementAt(0).Amount == 44);
                    var purchasesRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM StockPurchase");
                    Assert.True(purchasesRemaining.ToList()[0] == 1);
                }
            }
        }

        [Fact]
        public async Task Removing_position_should_remove_related_purchases()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var port1 = new Portfolio("abc123");
                    ctx.Portfolios.Add(port1);

                    var stock = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
                    ctx.Stocks.Add(stock);
                    await ctx.SaveChangesAsync();

                    port1.AddStockPurchase(stock, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
                    port1.AddStockPurchase(stock, DateTime.UtcNow.AddDays(-1), 44, 121, Common.Broker.InteractiveBrokers);
                    await ctx.SaveChangesAsync();

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    var positionIdToDel = portFromDb.Positions.ElementAt(0).PortfolioPositionId;
                    portFromDb.RemovePosition(positionIdToDel);
                    await ctx.SaveChangesAsync();

                    portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");

                    Assert.NotNull(portFromDb);
                    Assert.True(portFromDb.Positions.Count == 0);
                    var purchasesRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM StockPurchase");
                    Assert.True(purchasesRemaining.ToList()[0] == 0);
                    var positionsRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM PortfolioPosition");
                    Assert.True(positionsRemaining.ToList()[0] == 0);
                }
            }
        }

        [Fact]
        public async Task Removing_one_position_should_not_affect_others()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var port1 = new Portfolio("abc123");
                    ctx.Portfolios.Add(port1);

                    var stock1 = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
                    var stock2 = new Stock("ABC", "ABC Company", 456, 9.6, 15, 31.8, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector 2");
                    ctx.Stocks.Add(stock1);
                    ctx.Stocks.Add(stock2);
                    await ctx.SaveChangesAsync();

                    port1.AddStockPurchase(stock1, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
                    port1.AddStockPurchase(stock2, DateTime.UtcNow.AddDays(-1), 44, 121, Common.Broker.InteractiveBrokers);
                    await ctx.SaveChangesAsync();

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    var positionIdToDel = portFromDb.Positions.ElementAt(0).PortfolioPositionId;
                    portFromDb.RemovePosition(positionIdToDel);
                    await ctx.SaveChangesAsync();

                    portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");

                    Assert.NotNull(portFromDb);
                    Assert.True(portFromDb.Positions.Count == 1);
                    var purchasesRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM StockPurchase");
                    Assert.True(purchasesRemaining.ToList()[0] == 1);
                    var positionsRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM PortfolioPosition");
                    Assert.True(positionsRemaining.ToList()[0] == 1);
                }
            }
        }

        [Fact]
        public async Task Removing_portfolio_should_remove_related_positions_and_purchases()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var port1 = new Portfolio("abc123");
                    ctx.Portfolios.Add(port1);

                    var stock = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
                    ctx.Stocks.Add(stock);
                    await ctx.SaveChangesAsync();

                    port1.AddStockPurchase(stock, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
                    port1.AddStockPurchase(stock, DateTime.UtcNow.AddDays(-1), 44, 121, Common.Broker.InteractiveBrokers);
                    await ctx.SaveChangesAsync();

                    var portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");
                    ctx.Portfolios.Remove(portFromDb);
                    await ctx.SaveChangesAsync();

                    portFromDb = ctx.Portfolios.SingleOrDefault(p => p.UserId == "abc123");

                    Assert.Null(portFromDb);
                    var purchasesRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM StockPurchase");
                    Assert.True(purchasesRemaining.ToList()[0] == 0);
                    var positionsRemaining = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM PortfolioPosition");
                    Assert.True(positionsRemaining.ToList()[0] == 0);
                }
            }
        }



        private async Task SeedPortfolioAndStock(FinanceManagerContext ctx, string userId)
        {
            var port1 = new Portfolio(userId);
            ctx.Portfolios.Add(port1);

            var stock = new Stock("XYZ", "XYZ Company", 123, 5.6, 33, 12.5, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
            ctx.Stocks.Add(stock);
            await ctx.SaveChangesAsync();

            port1.AddStockPurchase(stock, DateTime.UtcNow, 42, 124, Common.Broker.InteractiveBrokers);
            await ctx.SaveChangesAsync();
        }
    }
}
