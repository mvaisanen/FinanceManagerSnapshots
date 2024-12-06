using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Common.Dtos;
using Common.HelperModels;
using NLog;
using Server.Mappings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceManager.Server.Database;
using Financemanager.Server.Database.Domain;
using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Models;

namespace Server.Services
{
    public class PortfolioService : IDisposable
    {
        private readonly FinanceManagerContext _ctx;
        private Logger _log;
        public readonly int InstanceNumber;
        private static int InstancesCreated;

        public PortfolioService(FinanceManagerContext ctx)
        {
            _log = LogManager.GetCurrentClassLogger();
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);
            _log.Debug($"PortfolioService #{InstanceNumber} created with FinanceManagerContext instance #{ctx.InstanceNumber}");
            _ctx = ctx;
        }

        public async Task<PortfolioDto> GetPortfolioByUserId(string userId)
        {
            var port = _ctx.Portfolios
                        .Include(po => po.Positions).ThenInclude(pos => pos.Buys)
                        .Include(po => po.Positions).ThenInclude(pos => pos.Stock).ThenInclude(s => s.SSDSafetyScore)
                        .Include(po => po.Positions).ThenInclude(pos => pos.Stock).ThenInclude(s => s.StockDataUpdateSources)
                        .FirstOrDefault(p => p.UserId ==userId);

            if (port == null)
                throw new ArgumentException("Cannot find portfolio for given user");
            
            return port.ToDTO();           
        }

        public async Task<PortfolioDto> AddPurchaseToPortfolio(string userId, AddToPortfolioDto purchase)
        {
            var portfolio = _ctx.Portfolios
                .Include(pf => pf.Positions).
                    ThenInclude(pos => pos.Buys)
                .Include(pf => pf.Positions)
                    .ThenInclude(pos => pos.Stock)
                .FirstOrDefault(p => p.Id == purchase.PortfolioId);

            _log.Debug($"Trying to add purchase {purchase} to Portfolio {portfolio?.Id}");

            if (portfolio == null || portfolio.UserId != userId) //if portfolio has different userid than the user that made the request, deny add
            {
                throw new ArgumentException("Invalid portfolio or access denied");
            }

            if (purchase.Price <= 0.0001)
            {
                throw new ArgumentException("Invalid price");
            }
            if (purchase.PurchaseDate < new DateTime(1920, 1, 1))
            {
                throw new ArgumentException("Invalid purchase date");
            }

            var stock = _ctx.Stocks.FirstOrDefault(s => s.Ticker == purchase.StockTicker.ToUpper());
            if (stock == null)
            {
                throw new ArgumentException("Invalid stock ticker");
            }

            portfolio.AddStockPurchase(stock, purchase.PurchaseDate, purchase.Amount, purchase.Price);

            await _ctx.SaveChangesAsync();
            return portfolio.ToDTO();
        }

        public async Task<PortfolioDto> UpdatePurchase(string userId, StockPurchaseDto purchase)
        {
            var portfolio = _ctx.Portfolios
                .Include(pf => pf.Positions).
                    ThenInclude(pos => pos.Buys)
                .Include(pf => pf.Positions)
                    .ThenInclude(pos => pos.Stock)
                .FirstOrDefault(p => p.Positions.Any(pos => pos.Buys.Any(buy => buy.StockPurchaseId == purchase.Id)));

            if (portfolio == null || portfolio.UserId != userId) //Dont let users edit portfolio they dont own, and ofc some portfolio must have this purchase
            {
                throw new ArgumentException("Invalid purchase or access denied");
            }

            portfolio.UpdatePurchase(purchase.Id, purchase.PurchaseDate, purchase.Amount, purchase.Price);

            await _ctx.SaveChangesAsync();
            return portfolio.ToDTO();
        }


        public async Task<PortfolioDto> DeletePosition(int positionId, int portfolioId, string userId)
        {
            var portfolio = _ctx.Portfolios
                .Include(pf => pf.Positions).
                    ThenInclude(pos => pos.Buys)
                .Include(pf => pf.Positions)
                    .ThenInclude(pos => pos.Stock)
                //.FirstOrDefault(p => p.UserId == userId && p.Id == portfolioId);
                .FirstOrDefault(p => p.Id == portfolioId);

            if (portfolio == null)
                throw new ArgumentException("Invalid portfolio id");
            else if (portfolio.UserId != userId)
                throw new ArgumentException("Invalid portfolio id (access denied)");

            var position = portfolio.Positions.FirstOrDefault(p => p.PortfolioPositionId == positionId);
            if (position != null)
            {
                //position.Buys.Clear();
                //portfolio.Positions.Remove(position);
                portfolio.RemovePosition(positionId);
                await _ctx.SaveChangesAsync();
                return portfolio.ToDTO();
            }
            else
            {
                throw new ArgumentException("Cannot find given position in users portfolio");
            }
        }

        public async Task<PortfolioDto> UploadUsersIbPortfolio(string userId, FileUpload uploadedFile)
        {
            _log.Debug($"UploadUsersIbPortfolio for user {userId} starting...");
            var portfolio = _ctx.Portfolios
                .Include(pf => pf.Positions).
                    ThenInclude(pos => pos.Buys)
                .Include(pf => pf.Positions)
                    .ThenInclude(pos => pos.Stock)
                .FirstOrDefault(p => p.UserId == userId);

            Dictionary<int, List<IbCsvPortfolioRow>> StockImportedBuys = new Dictionary<int, List<IbCsvPortfolioRow>>();
            
            var memStream = new MemoryStream(uploadedFile.Data);
            using (var file = new StreamReader(memStream))
            {
                var line = file.ReadLine();
                while (line != null)
                {
                    line = line.Replace("\"\"", "\""); //Replace possible double double quotation marks "" with jsut one double quotation mark: "
                    /*if (line.StartsWith('"'))
                        line = line.Substring(1); //Remove possible line-starting double quotation mark
                    if (line.EndsWith('"'))
                        line = line.Substring(0, line.Length - 1); //Remove possible line-ending double quotation mark*/

                    StringReader sr = new StringReader(line);
                    var parser = new TextFieldParser(sr);
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    string[] fields = { };
                    try
                    {
                        Console.WriteLine("trying to parse:" + line);
                        fields = parser.ReadFields();
                    }
                    catch (MalformedLineException mle)
                    {
                        _log.Warn($"UploadUsersIbPortfolio cannot parse line: {line}");
                        throw;
                    }

                    if (string.IsNullOrEmpty(fields[0]) || fields[0] != "Long Open Positions" || string.IsNullOrEmpty(fields[2]) || fields[2] != "Lot")
                    {
                        _log.Info($"UploadUsersIbPortfolio() skipping row {line}");
                        line = file.ReadLine();
                        continue;
                    }

                    var tick = fields[5];
                    var currency = fields[4];
                    //TODO: Instead of this trick, ui should perhaps allow user to manually check & modify the ticker. Problem is, ENB for example does have US ticker too...
                    if (currency.ToUpper() == "CAD" && !tick.EndsWith(".TO"))
                        tick = tick + ".TO"; //Canadian stocks

                    var stock = _ctx.Stocks.FirstOrDefault(s => s.Ticker == tick.ToUpper());
                    if (stock == null)
                    {
                        //Few special cases, for real use user should upload via website and be given option to fix ticker etc
                        if (tick == "TD")
                        {
                            tick = "TD.TO";
                            stock = _ctx.Stocks.FirstOrDefault(s => s.Ticker == tick.ToUpper());
                        }
                        else if (tick == "UNVB")
                        {
                            tick = "UNA.AS";
                            stock = _ctx.Stocks.FirstOrDefault(s => s.Ticker == tick.ToUpper());
                        }
                        else if (tick == "MUV2d")
                        {
                            tick = "MUV2.DE";
                            stock = _ctx.Stocks.FirstOrDefault(s => s.Ticker == tick.ToUpper());
                        }

                        if (stock == null)
                        {
                            //Even special case fixes wont work, log error
                            _log.Error($"Can't recognise stock {tick}, no stock found in database with that ticker");
                            throw new ArgumentException($"No stock {tick} found in database, cannot add purchase to portfolio.");
                        }
                    }

                    /*var position = portfolio.Positions.FirstOrDefault(pos => pos.Stock.Id == stock.Id); //Do we have existing position (old buys) for this stock purchase?
                    if (position == null)
                    {
                        position = new PortfolioPosition();
                        position.Stock = stock;
                        portfolio.Positions.Add(position);
                    }*/

                    _log.Debug("UploadUsersIbPortfolio trying to get quantity...");
                    double quantity = 0.0;
                    try
                    {
                        quantity = double.Parse(fields[7], CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        //quantity = double.Parse(sheet.GetRow(row).GetCell(7).StringCellValue, CultureInfo.InvariantCulture);
                        throw new ArgumentException($"Can't parse quantity for position {stock.Ticker} in line {line}");
                    }
                    if (quantity < 0.0001)
                        throw new ArgumentException($"Can't parse quantity for position {stock.Ticker} in line {line}");

                    _log.Debug("UploadUsersIbPortfolio trying to get date...");
                    var date = DateTime.ParseExact(fields[6], "yyyy-MM-dd, HH:mm:ss", CultureInfo.InvariantCulture);

                    _log.Debug($"UploadUsersIbPortfolio trying to read price (line content: {line})...");
                    var price = double.Parse(fields[9], CultureInfo.InvariantCulture);

                    /*_log.Debug("UploadUsersIbPortfolio has parsed values, adding to database if not exists...");

                    
                    //TODO: Check if date comparison works, maybe need to check if times are close enough
                    //TODO 2022-10-19: This doesnt work if order is filled in multiple stages of equal size: date and quantity will be equal, prices may differ a little
                    var existingBuy = position.Buys.FirstOrDefault(b => b.Amount == quantity && b.PurchaseDate == date);
                    if (existingBuy == null)
                    {
                        var buy = new StockPurchase();
                        buy.Amount = quantity;
                        buy.Price = price;
                        buy.PurchaseDate = date;
                        position.Buys.Add(buy);
                    }
                    */
                    if (StockImportedBuys.ContainsKey(stock.Id))
                        StockImportedBuys[stock.Id].Add(new IbCsvPortfolioRow() { Symbol = tick.ToUpper(), BuyDate = date, Price = price, Quantity = quantity });
                    else
                        StockImportedBuys.Add(stock.Id, new List<IbCsvPortfolioRow>() { new IbCsvPortfolioRow() { Symbol = tick.ToUpper(), BuyDate = date, Price = price, Quantity = quantity } });

                    line = file.ReadLine();
                }
            }

            //TODO: Go through ImportedRows and match existing one by one, making sure that import with a buy containing many identical date/amount buys still gets all buys imported
            foreach (var stockBuys in StockImportedBuys)
            {
                var position = portfolio.Positions.FirstOrDefault(pos => pos.Stock.Id == stockBuys.Key); //Do we have existing position (old buys) for this stock purchase?
                //if (position == null)
                //{
                //    position = new PortfolioPosition();
                //    position.Stock = _ctx.Stocks.First(s => s.Id == stockBuys.Key);
                //    portfolio.Positions.Add(position);
                //}

                var dbBuysCopy = position != null ? position.Buys.ToList() : new List<StockPurchase>(); //We want to remove items locally below upon matches, not from the database!
                foreach (var buy in stockBuys.Value)
                {
                    var existingBuy = dbBuysCopy.FirstOrDefault(b => b.Amount == buy.Quantity && b.PurchaseDate == buy.BuyDate);
                    if (existingBuy != null)
                        dbBuysCopy.Remove(existingBuy); //Marks the match as "used" in the list - this is NOT meant to remove anything from database - only from local list!
                    else
                    {
                        _log.Debug("No existing buy in database for this ib-csv buy, adding as new: " + buy.ToString());
                        //var newBuy = new StockPurchase() { Amount = buy.Quantity, Price = buy.Price, PurchaseDate = buy.BuyDate };
                        //position.Buys.Add(newBuy); //This goes to the database position (which will be saved/committed later)
                        var stock = _ctx.Stocks.SingleOrDefault(s => s.Id == stockBuys.Key);
                        portfolio.AddStockPurchase(stock, buy.BuyDate, buy.Quantity, buy.Price, Common.Broker.InteractiveBrokers);
                    }
                }

            }


            var res = await _ctx.SaveChangesAsync();
            _log.Debug($"IB portfolio uploaded, total of {res} changes done.");
            return portfolio.ToDTO();
        }


        public async void Dispose()
        {
            await _ctx.DisposeAsync();
        }




        private class IbCsvPortfolioRow 
        { 
            public double Price { get; set; }
            public string Symbol { get; set; }
            public DateTime BuyDate { get; set; }
            public double Quantity { get; set; }

            public override string ToString()
            {
                return $"Symbol: {Symbol}, BuyDate: {BuyDate}, Quantity: {Quantity}, Price: {Price}";
            }
        }
    }   
}
