using Akka.Event;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Common;
using NLog;
using NLog.Fluent;
using NPOI.SS.UserModel;
using FinanceManager.Server.Database;
using Server.Auth.Entities;
using Server.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Migrations;
using NPOI.OpenXmlFormats.Spreadsheet;
using System.Security.Claims;
using FinanceManager.Server.Database.Domain.Auth;
using Financemanager.Server.Database.Domain;
using Server.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server.Database
{

    public class FinanceManagerDemoSeeder
    {
        private readonly FinanceManagerContext _dbContext;
        private readonly AuthDbContext _authContext;
        private UserManager<AppUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private Logger _log;
        private readonly IConfiguration _configuration;
        //private RoleManager<AppRole> _roleManager;
        private readonly StockService _stockService;

        public FinanceManagerDemoSeeder(FinanceManagerContext dbContext, AuthDbContext authContext,
            UserManager<AppUser> userManager, IConfiguration configuration, RoleManager<IdentityRole> roleManager, StockService stockService)
        {
            _dbContext = dbContext;
            _authContext = authContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _log = LogManager.GetCurrentClassLogger();
            _configuration = configuration;
            _stockService = stockService;
        }
        
        public async Task Seed()
        {
            AppUser testUser = null;
            AppUser adminUser = null;
            if (!_authContext.Users.Any())
            {
                var userRole = new IdentityRole("User");
                await _roleManager.CreateAsync(userRole);
                await _roleManager.AddClaimAsync(userRole, new System.Security.Claims.Claim(ClaimTypes.Role, "User"));
                var adminRole = new IdentityRole("Admin");
                await _roleManager.CreateAsync(adminRole);
                await _roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(ClaimTypes.Role, "Admin"));
                await _roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(ClaimTypes.Role, "User")); //admin is also a user in this case

                adminUser = new AppUser() { UserName = "Administrator" };
                var result = await _userManager.CreateAsync(adminUser, "adminpw!");              
                await _userManager.AddToRoleAsync(adminUser, adminRole.Name);
         

                testUser = new AppUser() { UserName = "test" };
                var res = await _userManager.CreateAsync(testUser, "testpw!");
                await _userManager.AddToRoleAsync(testUser, userRole.Name);

                await _authContext.SaveChangesAsync();
            }
            else
            {
                testUser = _authContext.Users.First(u => u.UserName == "test");
                adminUser = _authContext.Users.First(u => u.UserName == "Administrator");
            }

            var exeFolder = System.Reflection.Assembly.GetEntryAssembly().Location;

            var file = _configuration.GetValue<string>($"AppSettings:UsCccExcel");
            var fileWithPath = $"{AppDomain.CurrentDomain.BaseDirectory}\\{file}";
            await _stockService.UploadUsCcc(file);

            _log.Debug("FinanceManagerSeeder adding basic finnish stocks...");
            AddBasicFinnishStockToDb("Fortum", "FORTUM.HE", 26.92, 1.12);
            AddBasicFinnishStockToDb("Kesko B", "KESKOB.HE", 31.19, 0.63);
            AddBasicFinnishStockToDb("Konecranes", "KCR.HE", 35.95, 1.20);
            AddBasicFinnishStockToDb("Neles", "NELES.HE", 11.81, 0.22);
            AddBasicFinnishStockToDb("Metsä Board B", "METSB.HE", 8.77, 0.24);
            AddBasicFinnishStockToDb("Nokian Renkaat", "TYRES.HE", 34.27, 0.79);
            AddBasicFinnishStockToDb("Nordea Bank", "NDA-FI.HE", 9.278, 0.40);
            AddBasicFinnishStockToDb("Rapala VMC", "RAP1V.HE", 8.58, 0.00);
            AddBasicFinnishStockToDb("Sampo A", "SAMPO.HE", 44.35, 1.50);
            AddBasicFinnishStockToDb("Valmet", "VALMT.HE", 36.70, 0.80);
            AddBasicFinnishStockToDb("Tokmanni", "TOKMAN.HE", 22.88, 0.85);

            var mu = new Stock("MUV2.DE", "Munich Re", 333.90, 11.60, 0, 23.41, Currency.EUR, Exchange.Frankfurt, DataUpdateSource.Manual, "Financial Services") { };
            _dbContext.Stocks.Add(mu);

            var stock1 = _dbContext.Stocks.FirstOrDefault(s => s.Ticker == "JNJ");
            var stock2 = _dbContext.Stocks.FirstOrDefault(s => s.Ticker == "LMT");
            var wList1 = new Watchlist(testUser.Id);
            wList1.AddOrUpdateStock(stock1);
            wList1.AddOrUpdateStock(stock2);
            _dbContext.Watchlists.Add(wList1);

            var portfolio1 = new Portfolio(testUser.Id);
            _dbContext.Portfolios.Add(portfolio1);
            await _dbContext.SaveChangesAsync();

             var pid = portfolio1.Id;
            _log.Debug("FinanceManagerDemoSeeder adding portfolio contents...");
            AddBuyToPortfolio(pid, "WMT", 71.1923, 210, DateTime.ParseExact("14.2.2018", "d.M.yyyy", null), Broker.Nordnet);
            AddBuyToPortfolio(pid, "KO", 39.1638, 300, DateTime.ParseExact("26.3.2019", "d.M.yyyy", null), Broker.Nordnet);
            AddBuyToPortfolio(pid, "KMI", 33.3442, 428, DateTime.ParseExact("29.4.2021", "d.M.yyyy", null), Broker.Nordnet);
            AddBuyToPortfolio(pid, "KMI", 39.6735, 350, DateTime.ParseExact("11.8.2022", "d.M.yyyy", null), Broker.Nordnet);
            AddBuyToPortfolio(pid, "LMT", 195.7822, 63, DateTime.ParseExact("5.2.2019", "d.M.yyyy", null), Broker.Nordnet);
            AddBuyToPortfolio(pid, "MUV2.DE", 163.37, 63, DateTime.ParseExact("1.7.2019", "d.M.yyyy", null), Broker.Nordnet);
            AddBuyToPortfolio(pid, "LMT", 275.93, 21, DateTime.ParseExact("13.11.2022", "d.M.yyyy", null), Broker.InteractiveBrokers);
            AddBuyToPortfolio(pid, "TXN", 145.93, 35, DateTime.ParseExact("3.7.2023", "d.M.yyyy", null), Broker.InteractiveBrokers);
            AddBuyToPortfolio(pid, "APD", 295.93, 21, DateTime.ParseExact("23.6.2024", "d.M.yyyy", null), Broker.InteractiveBrokers);
            await _dbContext.SaveChangesAsync();

            //Seed some dividends
            _dbContext.Dividends.Add(new ReceivedDividend(testUser.Id, DateTime.ParseExact("14.2.2019", "d.M.yyyy", null), "LMT", "LMT", null, null, 1120.74, Currency.USD, Broker.InteractiveBrokers, null));
            _dbContext.Dividends.Add(new ReceivedDividend(testUser.Id, DateTime.ParseExact("15.3.2020", "d.M.yyyy", null), "MMM", "MMM", null, null, 1520.74, Currency.USD, Broker.InteractiveBrokers, null));
            _dbContext.Dividends.Add(new ReceivedDividend(testUser.Id, DateTime.ParseExact("16.4.2021", "d.M.yyyy", null), "CVX", "CVX", null, null, 1920.74, Currency.USD, Broker.InteractiveBrokers, null));
            _dbContext.Dividends.Add(new ReceivedDividend(testUser.Id, DateTime.ParseExact("17.5.2022", "d.M.yyyy", null), "APD", "APD", null, null, 2120.74, Currency.USD, Broker.InteractiveBrokers, null));
            _dbContext.Dividends.Add(new ReceivedDividend(testUser.Id, DateTime.ParseExact("18.6.2023", "d.M.yyyy", null), "ADP", "ADP", null, null, 2520.74, Currency.USD, Broker.InteractiveBrokers, null));
            _dbContext.Dividends.Add(new ReceivedDividend(testUser.Id, DateTime.ParseExact("19.7.2024", "d.M.yyyy", null), "MSFT", "MSFT", null, null, 1400.74, Currency.USD, Broker.InteractiveBrokers, null));
            _dbContext.Dividends.Add(new ReceivedDividend(testUser.Id, DateTime.ParseExact("19.9.2024", "d.M.yyyy", null), "FORTUM.HE", "FORTUM.HE", null, null, 1420.34, Currency.EUR, Broker.Nordnet, null));
            await _dbContext.SaveChangesAsync();

            var thePlan = new DividendsPlan() { UserId = testUser.Id, StartYear = 2019, Years = 23, StartDividends = 1120.74, Yield = 3, CurrentDivGrowth = 3.5, NewDivGrowth = 6.5, YearlyInvestment = 8000 };
            _dbContext.DividendPlans.Add(thePlan);
            await _dbContext.SaveChangesAsync();
        }


        private void LoadIbPortfolioCsv(string fileWithPath, string userId)
        {
            var portfolio = _dbContext.Portfolios
                .Include(pf => pf.Positions).
                    ThenInclude(pos => pos.Buys)
                .Include(pf => pf.Positions)
                    .ThenInclude(pos => pos.Stock)
                .FirstOrDefault(p => p.UserId == userId);

            using (var file = new System.IO.StreamReader(fileWithPath))
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
                        Log.Warn($"Cannot parse line: {line}");
                        return;
                    }

                    /*var firstCellInRow = fields[0];
                    if (firstCellInRow == null || firstCellInRow.StringCellValue == null || firstCellInRow.StringCellValue != "Long Open Positions")
                        continue;
                    var fieldNameCell = sheet.GetRow(row).GetCell(2);
                    if (fieldNameCell == null || fieldNameCell.StringCellValue != "Lot")
                        continue;*/

                    if (string.IsNullOrEmpty(fields[0]) || fields[0] != "Long Open Positions" || string.IsNullOrEmpty(fields[2]) || fields[2] != "Lot")
                    {
                        _log.Info($"LoadIbPortfolioCsv() skipping row {line}");
                        line = file.ReadLine();
                        continue;
                    }

                    //var tick = sheet.GetRow(row).GetCell(5).StringCellValue;
                    var tick = fields[5];

                    var currency = fields[4];
                    //TODO: Instead of this trick, ui should perhaps allow user to manually check & modify the ticker. Problem is, ENB for example does have US ticker too...
                    if (currency.ToUpper() == "CAD" && !tick.EndsWith(".TO"))
                        tick = tick + ".TO"; //Canadian stocks

                    var stock = _dbContext.Stocks.FirstOrDefault(s => s.Ticker == tick.ToUpper());
                    if (stock == null)
                    {
                        //Few special cases, for real use user should upload via website and be given option to fix ticker etc
                        if (tick == "TD")
                        {
                            tick = "TD.TO";
                            stock = _dbContext.Stocks.FirstOrDefault(s => s.Ticker == tick.ToUpper());
                        }
                        else if (tick == "UNVB")
                        {
                            tick = "UNA.AS";
                            stock = _dbContext.Stocks.FirstOrDefault(s => s.Ticker == tick.ToUpper());
                        }

                        if (stock == null)
                        {
                            //Even special case fixes wont work, log error
                            _log.Error($"Can't seed stock {tick}, no stock found in database with that ticker");
                            throw new ArgumentException($"No stock {tick} found in database, cannot seed portfolio.");
                        }
                    }

                    /*var position = portfolio.Positions.FirstOrDefault(pos => pos.Stock.Id == stock.Id); //Do we have existing position (old buys) for this stock purchase?
                    if (position == null)
                    {

                        position = new PortfolioPosition();
                        position.Stock = stock;
                        portfolio.Positions.Add(position);
                    }*/

                    _log.Debug("Seeder trying to get quantity...");
                    double quantity = 0.0;
                    try
                    {
                        //quantity = sheet.GetRow(row).GetCell(7).NumericCellValue;
                        quantity = double.Parse(fields[7], CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        //quantity = double.Parse(sheet.GetRow(row).GetCell(7).StringCellValue, CultureInfo.InvariantCulture);
                        throw new ArgumentException($"Can't parse quantity for position {stock.Ticker} in line {line}");
                    }
                    if (quantity < 0.0001)
                        throw new ArgumentException($"Can't parse quantity for position {stock.Ticker} in line {line}");

                    _log.Debug("Seeder trying to get date...");
                    /*var dateCellStr = sheet.GetRow(row).GetCell(6).StringCellValue;
                    if (dateCellStr == "-")
                        continue; //Some sort of summary / extra row, no date
                    var dateStr = dateCellStr.Substring(0, 10);
                    _log.Debug($"Seeder trying to parse date from: {dateStr}");
                    var date = DateTime.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);*/
                    var date = DateTime.ParseExact(fields[6], "yyyy-MM-dd, HH:mm:ss", CultureInfo.InvariantCulture);

                    _log.Debug($"Seeder trying to read price (line content: {line})...");
                    /*var priceStr = sheet.GetRow(row).GetCell(9).StringCellValue;
                    _log.Debug($"Seeder trying to parse price: {priceStr}");
                    var price = double.Parse(priceStr, CultureInfo.InvariantCulture);*/
                    var price = double.Parse(fields[9], CultureInfo.InvariantCulture);


                    _log.Debug("Seeder has parsed values, adding to database...");
                    portfolio.AddStockPurchase(stock, date, quantity, price, Broker.InteractiveBrokers);

                    line = file.ReadLine();
                }
            }           
        }

        private void AddBasicFinnishStockToDb(string companyName, string ticker, double currentPrice, double dividend, PriceSource psource = PriceSource.Kauppalehti)
        {
            Stock s = new Stock(ticker, companyName, currentPrice, dividend, 0, 0.001, Currency.EUR, Exchange.Helsinki, DataUpdateSource.Manual);
            _dbContext.Stocks.Add(s);
        }

        private void AddBuyToPortfolio(int portfolioId, string ticker, double purchasePrice, int shares, DateTime purchaseDate, Broker broker)
        {
            Stock s = _dbContext.Stocks.FirstOrDefault(st => st.Ticker == ticker);
            if (s == null)
                throw new ArgumentException("No stock with ticker " + ticker + " in database! Init failed.");
            Portfolio portfolio = _dbContext.Portfolios.First(p => p.Id == portfolioId);
            portfolio.AddStockPurchase(s, purchaseDate, shares, purchasePrice, broker);
        }

        private void LoadIbDividends(string fileWithPath, bool MultiAccount, string userId, FinanceManagerContext ctx)
        {
            int offset = MultiAccount == true ? 1 : 0;
            var divsToAdd = new List<ReceivedDividend>();

            using (var file = new System.IO.StreamReader(fileWithPath))
            {
                var line = file.ReadLine();
                while (line != null)
                {
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
                        Log.Warn($"Cannot parse line: {line}");
                        return;
                    }

                    //Skip non-data and total rows
                    if (string.IsNullOrEmpty(fields[0]) || fields[0] != "Dividends" || fields[1] == "Header" || string.IsNullOrEmpty(fields[2]) || fields[2].Contains("Total"))
                    {
                        _log.Info($"LoadIbDividends() skipping row {line}");
                        line = file.ReadLine();
                        continue;
                    }

                    //Dividends,Data,USD,2020-01-07,PEP(US7134481081) Cash Dividend USD 0.955 per Share (Ordinary Dividend),12.42,
                    var currencyOk = Enum.TryParse(typeof(Currency), fields[2 + offset], out var currency);
                    if (!currencyOk)
                        throw new ArgumentException($"Invalid currency: {fields[2 + offset]}");

                    var dateStr = fields[3 + offset];
                    var date = DateTime.Parse(dateStr);

                    var tickerAndInfo = fields[4 + offset];
                    var ticker = tickerAndInfo.Substring(0, tickerAndInfo.IndexOf('('));

                    var totalPayment = double.Parse(fields[5 + offset], CultureInfo.InvariantCulture);

                    var div = new ReceivedDividend(userId, date, ticker, ticker, null, null, totalPayment, (Currency)currency, Broker.InteractiveBrokers, null);
                    div.Broker = Broker.InteractiveBrokers;

                    if (line.Contains("Reversal"))
                    {
                        var match = divsToAdd.FirstOrDefault(d => d.CompanyTicker == ticker && d.PaymentDate == date && d.Currency == (Currency)currency && d.Broker == Broker.InteractiveBrokers);
                        if (match != null)
                        {
                            _log.Info($"Removing reverted dividend {ticker} {date} {totalPayment} {(Currency)currency}");
                            divsToAdd.Remove(match);
                        }
                        else
                        {
                            throw new ArgumentException("Reverted dividend that cannot be found!"); //todo: check earlier divs from database, not just on current file(s)?
                        }
                    }
                    else
                    {
                        divsToAdd.Add(div);
                    }

                    line = file.ReadLine();
                }

                foreach (var div in divsToAdd)
                {
                    ctx.Dividends.Add(div);
                }
            }
        }

        private void LoadNordnetTransactions(string fileWithPath, string userId, FinanceManagerContext ctx) //Just dividends atm
        {
            //Note: Assumes user's home currency to be EUR
            _log.Debug("Starting to process Nordnet transactions for dividends...");

            using (var file = new System.IO.StreamReader(fileWithPath))
            {
                //var line = file.ReadLine();
                string? line;
                while ((line = file.ReadLine()) != null)
                {
                    var fields = line.Split(';');
                    if (fields.Length < 20)
                        continue;

                    var type = fields[4];
                    var invalidatedDate = fields[20];
                    if (type != "OSINKO" || invalidatedDate != "")
                        continue;
                    //TODO: Reduce hardcoding here?
                    var dateStr = fields[3]; //Using the payment date here
                    var tickerStr = fields[5];
                    var sharesStr = fields[8];
                    var divPerShareStr = fields[9];
                    var fxRateStr = fields[18];
                    var currencyStr = fields[13];
                    var totalStr = fields[12].Trim();

                    var date = DateTime.Parse(dateStr);
                    Currency currency;
                    var curValid = Enum.TryParse<Currency>(currencyStr, out currency);
                    double? rate = null;
                    if (curValid && currency == Currency.EUR)
                        rate = 1.0;
                    else if (curValid)
                    {
                        bool rateOk = double.TryParse(fxRateStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var nordnetRate);
                        if (rateOk)
                            rate = 1 / nordnetRate;
                    }
                    //rate = _fxRef.Ask<double?>(CurrencyMessage.NewGetRateForDay(currency, date)).Result;

                    ReceivedDividend current = ctx.Dividends.FirstOrDefault(cd =>
                        cd.UserId == userId &&
                        cd.Broker == Broker.Nordnet &&
                        cd.PaymentDate.Day == date.Day &&
                        cd.PaymentDate.Month == date.Month &&
                        cd.PaymentDate.Year == date.Year &&
                        cd.CompanyTicker == tickerStr
                        );
                    //TODO: päivitä jos tarpeen tai lisää uusi
                    if (current != null)
                    {
                        //Päivitä arvot?
                        _log.Warn($"Found existing dividend with id {current.Id} - skipping adding new");
                    }
                    else
                    {
                        var dividend = new ReceivedDividend();
                        dividend.AmountPerShare = Convert.ToDouble(divPerShareStr.Replace(',', '.'), CultureInfo.InvariantCulture);
                        dividend.CompanyTicker = tickerStr;
                        dividend.Currency = currency;
                        dividend.FxRate = rate;
                        dividend.ShareCount = Convert.ToInt32(sharesStr);
                        dividend.PaymentDate = date;
                        dividend.UserId = userId;
                        dividend.TotalReceived = Convert.ToDouble(totalStr.Replace(',', '.'), CultureInfo.InvariantCulture);
                        dividend.Broker = Broker.Nordnet;

                        ctx.Dividends.Add(dividend);
                    }
                }              
            }
            _log.Debug("Nordnet transactions loaded");
        }

    }
}
