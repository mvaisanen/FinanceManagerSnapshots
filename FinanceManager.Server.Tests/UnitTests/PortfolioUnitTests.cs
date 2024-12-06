using Akka.IO;
using Common.Dtos;
using Common.HelperModels;
using Financemanager.Server.Database.Domain;
using FinanceManager.Server.Database;
using FinanceManager.Server.Tests.Util;
using Microsoft.EntityFrameworkCore;
using Server.Controllers;
using Server.Mappings;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.Tests.UnitTests
{
    public class PortfolioUnitTests : IClassFixture<TestFmDatabaseFixture>
    {
        public TestFmDatabaseFixture Fixture { get; }
        public PortfolioUnitTests(TestFmDatabaseFixture fixture)
        {
            Fixture = fixture;
        }


        [Fact]
        public async Task Controller_get_portfolio_by_user_id_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1 = portfolios[0];

                    var mockAuthService = new MockAuthService(port1.UserId);
                    var portfolioService = new PortfolioService(ctx);

                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port1.UserId, "user1");

                    var portDto = await controller.PortfolioByUserId();
                    Assert.NotNull(portDto);
                    Assert.Equal(portDto, port1.ToDTO(), new PortfolioDtoEqualityComparer());


                    //Assert.True(portDto.Positions.Count == port1.Positions.Count);                    
                    //Assert.True(portDto.Positions.ElementAt(0).Stock.Ticker == port1.Positions.ElementAt(0).Stock.Ticker);
                    //Assert.True(portDto.Positions.ElementAt(0).Buys.Count == port1.Positions.ElementAt(0).Buys.Count);
                    //Assert.Equal(portDto.Positions.ElementAt(0).Buys.ElementAt(0), port1.Positions.ElementAt(0).Buys.ElementAt(0).ToDTO(), new StockPurchaseDtoEqualityComparer());
                    //Assert.Equal(portDto.Positions.ElementAt(0).Buys.ElementAt(1), port1.Positions.ElementAt(0).Buys.ElementAt(1).ToDTO(), new StockPurchaseDtoEqualityComparer());
                }
            }
        }


        [Fact]
        public async Task Controller_adding_purchase_to_existing_position_in_portfolio_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1 = portfolios[0];
                    var origPort = portfolios[0].ToDTO();

                    var mockAuthService = new MockAuthService(port1.UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port1.UserId, "user1");

                    var add = new AddToPortfolioDto() { StockTicker = port1.Positions.ElementAt(0).Stock.Ticker, Amount = 66, PortfolioId = port1.Id, Price = 144, PurchaseDate = DateTime.UtcNow }; //Add one buy to 1st position

                    var portDto = await controller.AddToPortfolio(add);

                    Assert.NotNull(portDto);
                    Assert.Equal(portDto.Positions.Count, origPort.Positions.Count); //Position count unchanged
                    Assert.True(portDto.Positions.ElementAt(0).Buys.Count == origPort.Positions.ElementAt(0).Buys.Count + 1); //Added one buy to first position
                    Assert.NotNull(portDto.Positions.ElementAt(0).Buys.SingleOrDefault(b => b.Amount == add.Amount && b.Price == add.Price && b.PurchaseDate == add.PurchaseDate));
                }
            }
        }

        [Fact]
        public async Task Controller_adding_purchase_to_other_users_portfolio_should_not_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1Dto = portfolios[0].ToDTO();
                    var port2UserId = portfolios[1].UserId;

                    var mockAuthService = new MockAuthService(port2UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port2UserId, "user2");

                    var add = new AddToPortfolioDto() { StockTicker = port1Dto.Positions.ElementAt(0).Stock.Ticker, Amount = 66, PortfolioId = port1Dto.Id, Price = 144, PurchaseDate = DateTime.UtcNow }; //Add one buy to 1st position

                    await Assert.ThrowsAsync<System.ArgumentException>( async () => await controller.AddToPortfolio(add));
                    var posPurchasesInDb = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM StockPurchase WHERE PortfolioPositionId = {port1Dto.Positions.ElementAt(0).Id}").ToList()[0];
                    Assert.Equal(posPurchasesInDb, port1Dto.Positions.ElementAt(0).Buys.Count); //No buys should have been added
                }
            }
        }

        [Fact]
        public async Task Controller_adding_purchase_with_non_existing_ticker_should_not_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1Dto = portfolios[0].ToDTO();
                    var port2UserId = portfolios[1].UserId;

                    var mockAuthService = new MockAuthService(port2UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port2UserId, "user2");

                    var add = new AddToPortfolioDto() { StockTicker = "NOGO", Amount = 66, PortfolioId = port1Dto.Id, Price = 144, PurchaseDate = DateTime.UtcNow }; //Add one buy to nonexisting stock

                    await Assert.ThrowsAsync<System.ArgumentException>(async () => await controller.AddToPortfolio(add));
                    var posPurchasesInDb = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM StockPurchase WHERE PortfolioPositionId = {port1Dto.Positions.ElementAt(0).Id}").ToList()[0];
                    Assert.Equal(posPurchasesInDb, port1Dto.Positions.ElementAt(0).Buys.Count); //No buys should have been added
                }
            }
        }

        [Fact]
        public async Task Controller_updating_purchase_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1Dto = portfolios[0].ToDTO();
                    var port1UserId = portfolios[0].UserId;

                    var mockAuthService = new MockAuthService(port1UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port1UserId, "user1");

                    var upd = port1Dto.Positions[0].Buys[0]; //Test will modify this purchase
                    upd.Amount = upd.Amount + 5;
                    upd.Price = upd.Price + 2.5;
                    upd.PurchaseDate = DateTime.UtcNow.AddMinutes(-10);

                    var updatedPortDto = await controller.UpdatePurchase(upd.Id, upd);
                    Assert.Equal(port1Dto.Positions.Count, updatedPortDto.Positions.Count); //No change in position count
                    Assert.Equal(port1Dto.Positions[0].Buys.Count, updatedPortDto.Positions[0].Buys.Count); //No change in the count of buys on the position of which one buy we modified
                    Assert.Equal(upd.Amount, updatedPortDto.Positions[0].Buys[0].Amount);
                    Assert.Equal(upd.Price, updatedPortDto.Positions[0].Buys[0].Price);
                    Assert.Equal(upd.PurchaseDate, updatedPortDto.Positions[0].Buys[0].PurchaseDate); 
                    var updatedBuyInDb = ctx.Database.SqlQuery<StockPurchase>($"SELECT * FROM StockPurchase WHERE StockPurchaseId = {upd.Id}").ToList()[0];
                    Assert.NotNull(updatedBuyInDb);
                    Assert.Equal(upd.Amount, updatedBuyInDb.Amount);
                    Assert.Equal(upd.Price, updatedBuyInDb.Price);
                    Assert.Equal(upd.PurchaseDate, updatedBuyInDb.PurchaseDate);
                    //Other positions/purchases should not change, testing one
                    Assert.Equal(port1Dto.Positions[1].Buys[0].Amount, updatedPortDto.Positions[1].Buys[0].Amount);
                    Assert.Equal(port1Dto.Positions[1].Buys[0].Price, updatedPortDto.Positions[1].Buys[0].Price);
                }
            }
        }

        [Fact]
        public async Task Controller_updating_other_users_purchase_should_not_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1Dto = portfolios[0].ToDTO();
                    var port2UserId = portfolios[1].UserId;

                    var mockAuthService = new MockAuthService(port2UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port2UserId, "user2");

                    var upd = port1Dto.Positions[0].Buys[0].DeepCopy(); //Test will try to modify this purchase
                    upd.Amount = upd.Amount + 5;
                    upd.Price = upd.Price + 2.5;
                    upd.PurchaseDate = DateTime.UtcNow.AddMinutes(-10);

                    await Assert.ThrowsAsync<ArgumentException>(async () => await controller.UpdatePurchase(upd.Id, upd));
                    //Make sure purchase was not modified
                    var buyInDb = ctx.Database.SqlQuery<StockPurchase>($"SELECT * FROM StockPurchase WHERE StockPurchaseId = {upd.Id}").ToList()[0];
                    Assert.NotNull(buyInDb);
                    Assert.Equal(port1Dto.Positions[0].Buys[0].Amount, buyInDb.Amount);
                    Assert.Equal(port1Dto.Positions[0].Buys[0].Price, buyInDb.Price);
                    Assert.Equal(port1Dto.Positions[0].Buys[0].PurchaseDate, buyInDb.PurchaseDate);

                }
            }
        }

        [Fact]
        public async Task Controller_deleting_position_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1Dto = portfolios[0].ToDTO();
                    var port1UserId = portfolios[0].UserId;

                    var mockAuthService = new MockAuthService(port1UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port1UserId, "user1");

                    var del = port1Dto.Positions[0]; //Test will delete this position

                    var updatedPortDto = await controller.DeletePosition(del.Id, port1Dto.Id);
                    Assert.Equal(port1Dto.Positions.Count-1, updatedPortDto.Positions.Count); //One position removed
                    Assert.DoesNotContain(updatedPortDto.Positions.ToList(), pos => pos.Id == del.Id);
                    Assert.Equal(port1Dto.Positions[1].Buys.Count, updatedPortDto.Positions[0].Buys.Count); //No change in the count of buys on the position remaining
                    var positionsInDb = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM PortfolioPosition WHERE PortfolioId = {port1Dto.Id}").ToList()[0];
                    Assert.Equal(positionsInDb, updatedPortDto.Positions.Count);
                    Assert.Equal(positionsInDb, port1Dto.Positions.Count-1);
                }
            }
        }

        [Fact]
        public async Task Controller_deleting_other_users_position_should_not_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1Dto = portfolios[0].ToDTO();
                    var port2UserId = portfolios[1].UserId;

                    var mockAuthService = new MockAuthService(port2UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port2UserId, "user2");

                    var del = port1Dto.Positions[0]; //Test will delete this position

                    await Assert.ThrowsAsync<ArgumentException>(async () => await controller.DeletePosition(del.Id, port1Dto.Id));
                    var positionsInDb = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM PortfolioPosition WHERE PortfolioId = {port1Dto.Id}").ToList()[0];
                    Assert.Equal(port1Dto.Positions.Count, positionsInDb);
                    var exactPosInDb = ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM PortfolioPosition WHERE PortfolioPositionId = {del.Id}").ToList()[0];
                    Assert.Equal(1, exactPosInDb);
                }
            }
        }

        [Fact]
        public async Task Controller_uploading_ib_portfolio_should_work()
        {
            using (var ctx = Fixture.CreateContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    var portfolios = await SeedPortfoliosAndStocks(ctx);
                    var port1Dto = portfolios[0].ToDTO();
                    var port1UserId = portfolios[0].UserId;

                    var mockAuthService = new MockAuthService(port1UserId);
                    var portfolioService = new PortfolioService(ctx);
                    var controller = new PortfolioController(mockAuthService, portfolioService).WithIdentity(port1UserId, "user1");

                    var lines = @"
Statement,Header,Field Name,Field Value
Statement,Data,BrokerName,
Statement,Data,BrokerAddress,
Statement,Data,Title,Activity Statement
Statement,Data,Period,""July 25, 2023""
Statement,Data,WhenGenerated,""2023-07-26, 09:47:33 EDT""
Long Open Positions,Header,DataDiscriminator,Asset Category,Currency,Symbol,Open,Quantity,Mult,Cost Price,Cost Basis,Close Price,Value,Unrealized P/L,% of NAV,Code
Long Open Positions,Data,Summary,Stocks,CAD,ENB,-,154,1,51.833706623,7982.39082,49.35,7599.9,-382.49082,4.65,
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2023-07-07, 15:39:46"",15,,48.504246667,727.5637,49.35,740.25,12.6863,,ST
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2023-02-10, 15:10:44"",13,,54.025103077,702.32634,49.35,641.55,-60.77634,,ST
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2022-08-17, 13:56:36"",12,,55.341513333,664.09816,49.35,592.2,-71.89816,,ST
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2022-03-02, 11:57:59"",14,,56.144408571,786.02172,49.35,690.9,-95.12172,,LT
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2022-02-14, 12:52:03"",14,,52.884408571,740.38172,49.35,690.9,-49.48172,,LT
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2021-12-16, 12:12:14"",23,,48.054758261,1105.25944,49.35,1135.05,29.79056,,LT
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2021-11-05, 11:58:05"",20,,53.80128,1076.0256,49.35,987,-89.0256,,LT
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2021-10-05, 13:12:01"",20,,50.63128,1012.6256,49.35,987,-25.6256,,LT
Long Open Positions,Data,Lot,Stocks,CAD,ENB,""2021-09-28, 10:50:14"",23,,50.786458261,1168.08854,49.35,1135.05,-33.03854,,LT
Long Open Positions,Data,Summary,Stocks,CAD,FTS,-,88,1,55.394912955,4874.75234,57.4,5051.2,176.44766,3.09,
Long Open Positions,Data,Lot,Stocks,CAD,FTS,""2023-07-07, 15:13:00"",13,,56.149903077,729.94874,57.4,746.2,16.25126,,ST
Long Open Positions,Data,Lot,Stocks,CAD,FTS,""2022-10-11, 11:49:30"",13,,50.469903077,656.10874,57.4,746.2,90.09126,,ST
Long Open Positions,Data,Lot,Stocks,CAD,FTS,""2022-09-29, 15:55:58"",13,,53.025103077,689.32634,57.4,746.2,56.87366,,ST
Long Open Positions,Data,Lot,Stocks,CAD,FTS,""2022-06-13, 10:24:06"",11,,61.678889091,678.46778,57.4,631.4,-47.06778,,LT
Long Open Positions,Data,Lot,Stocks,CAD,FTS,""2022-02-14, 12:41:30"",13,,57.239903077,744.11874,57.4,746.2,2.08126,,LT
Long Open Positions,Data,Lot,Stocks,CAD,FTS,""2021-10-27, 15:46:16"",25,,55.07128,1376.782,57.4,1435,58.218,,LT
Long Open Positions,Data,Summary,Stocks,CAD,TD,-,115,1,68.555481913,7883.88042,85.23,9801.45,1917.56958,6.00,
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2023-04-11, 13:32:56"",9,,81.289991111,731.60992,85.23,767.07,35.46008,,ST
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2023-03-14, 15:30:10"",9,,81.034091111,729.30682,85.23,767.07,37.76318,,ST
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2022-06-28, 14:25:17"",8,,84.46798,675.74384,85.23,681.84,6.09616,,LT
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2021-10-05, 13:28:54"",14,,85.872708571,1202.21792,85.23,1193.22,-8.99792,,LT
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2020-10-26, 15:42:06"",13,,59.658203077,775.55664,85.23,1107.99,332.43336,,LT
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2020-05-22, 11:09:50"",20,,55.10128,1102.0256,85.23,1704.6,602.5744,,LT
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2020-05-13, 15:51:30"",14,,54.692708571,765.69792,85.23,1193.22,427.52208,,LT
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2020-02-28, 15:11:03"",11,,68.752189091,756.27408,85.23,937.53,181.25592,,LT
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2018-03-27, 13:40:54"",11,,73.129789091,804.42768,85.23,937.53,133.10232,,LT
Long Open Positions,Data,Lot,Stocks,CAD,TD,""2016-08-18, 14:58:43"",6,,56.836666667,341.02,85.23,511.38,170.36,,LT
Long Open Positions,Total,,Stocks,CAD,,,,,,20741.02358,,22452.55,1711.52642,13.74,
Long Open Positions,Total,,Stocks,EUR,,,,,,14242.65348215,,15417.9415595,1175.28807735,,
Long Open Positions,Data,Summary,Stocks,EUR,MUV2d,-,9,1,244.3,2198.7,342,3078,879.3,2.74,
Long Open Positions,Data,Lot,Stocks,EUR,MUV2d,""2022-08-31, 11:12:43"",2,,238.425,476.85,342,684,207.15,,ST
Long Open Positions,Data,Lot,Stocks,EUR,MUV2d,""2022-02-24, 04:46:30"",7,,245.978571429,1721.85,342,2394,672.15,,LT
Long Open Positions,Data,Summary,Stocks,EUR,UNVB,-,60,1,42.412954117,2544.777247,48.8,2928,383.222753,2.61,
Long Open Positions,Data,Lot,Stocks,EUR,UNVB,""2021-02-04, 11:18:44"",11,,46.667760091,513.345361,48.8,536.8,23.454639,,LT
Long Open Positions,Data,Lot,Stocks,EUR,UNVB,""2020-03-24, 10:23:03"",12,,41.599996583,499.199959,48.8,585.6,86.400041,,LT
Long Open Positions,Data,Lot,Stocks,EUR,UNVB,""2020-03-12, 12:15:18"",12,,42.557388333,510.68866,48.8,585.6,74.91134,,LT
Long Open Positions,Data,Lot,Stocks,EUR,UNVB,""2018-05-03, 10:28:01"",11,,45.875254364,504.627798,48.8,536.8,32.172202,,LT
Long Open Positions,Data,Lot,Stocks,EUR,UNVB,""2016-11-14, 04:34:37"",14,,36.9225335,516.915469,48.8,683.2,166.284531,,LT
Long Open Positions,Total,,Stocks,EUR,,,,,,4743.477247,,6006,1262.522753,5.35,
Long Open Positions,Data,Summary,Stocks,USD,ADP,-,21,1,141.62400881,2974.104185,240.48,5050.08,2075.975815,4.07,
Long Open Positions,Data,Lot,Stocks,USD,ADP,""2023-05-24, 14:01:50"",3,,215.004852333,645.014557,240.48,721.44,76.425443,,ST
Long Open Positions,Data,Lot,Stocks,USD,ADP,""2023-05-09, 15:36:51"",3,,214.036952333,642.110857,240.48,721.44,79.329143,,ST
Long Open Positions,Data,Lot,Stocks,USD,ADP,""2020-03-23, 12:21:55"",5,,107.3102514,536.551257,240.48,1202.4,665.848743,,LT
Long Open Positions,Data,Lot,Stocks,USD,ADP,""2020-03-20, 11:59:52"",5,,113.1402514,565.701257,240.48,1202.4,636.698743,,LT
Long Open Positions,Data,Lot,Stocks,USD,ADP,""2020-03-19, 13:38:10"",5,,116.9452514,584.726257,240.48,1202.4,617.673743,,LT
";

                    var upload = new FileUpload() { };
                    //...
                }
            }
        }


        private async Task<List<Portfolio>> SeedPortfoliosAndStocks(FinanceManagerContext ctx)
        {
            var port1 = new Portfolio("user1-id");
            var port2 = new Portfolio("user2-id");
            ctx.Portfolios.AddRange(port1, port2);

            var stock1 = new Stock("XYZ1", "XYZ1 Company", 121, 5.1, 31, 12.1, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
            var stock2= new Stock("XYZ2", "XYZ2 Company", 122, 5.2, 32, 12.2, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
            var stock3 = new Stock("XYZ3", "XYZ3 Company", 123, 5.3, 33, 12.3, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
            var stock4 = new Stock("XYZ4", "XYZ4 Company", 124, 5.4, 34, 12.4, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.US_CCC, "Test sector");
            ctx.Stocks.AddRange(stock1, stock2, stock3, stock4);
            await ctx.SaveChangesAsync();

            port1.AddStockPurchase(stock1, DateTime.UtcNow, 41, 111, Common.Broker.InteractiveBrokers);
            port1.AddStockPurchase(stock2, DateTime.UtcNow, 42, 112, Common.Broker.InteractiveBrokers);
            port2.AddStockPurchase(stock3, DateTime.UtcNow, 43, 113, Common.Broker.InteractiveBrokers);
            port2.AddStockPurchase(stock4, DateTime.UtcNow, 44, 114, Common.Broker.InteractiveBrokers);
            await ctx.SaveChangesAsync();

            return new List<Portfolio> { port1, port2 };
        }
    }
}
