using Akka.Actor;
using Akka.Event;
using Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Common.Dtos;
using Server.Database;
using Server.Mappings;
using Server.Models;
using Financemanager.Server.Database.Domain;
using Server.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using Common;
using System.Text;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Net;
using System.Net.Http;
//using Newtonsoft.Json;
using System.Dynamic;
using System.Text.Json;
using NLog.Fluent;
using System.Security.Cryptography.Xml;
using FinanceManager.Server.Database;

namespace Server.Actors
{
    public class StockActor: ReceiveActor
    {
        IServiceScopeFactory _serviceScopeFactory;
        ILoggingAdapter _log;
        private List<DateTime> AlphaVantageCallTimes = new List<DateTime>();
        private List<DateTime> IexCallTimes = new List<DateTime>();

        public StockActor(IServiceScopeFactory serviceScopeFactory)
        {
            System.Diagnostics.Debug.WriteLine("StockActor startup...");
            _serviceScopeFactory = serviceScopeFactory;
            _log = Context.GetLogger();
            Become(Ready);
        }

        private void Ready()
        {
            Receive<StockMessage.GetClosestMatches>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    List<Tuple<Stock, int>> bestTickerCandidates = new List<Tuple<Stock, int>>();
                    List<Tuple<Stock, int>> bestNameCandidates = new List<Tuple<Stock, int>>();
                    
                    foreach (var stock in ctx.Stocks)
                    {
                        //_log.Debug($"GetClosestMatches handling stock {stock.Ticker} ({stock.Name})");

                        if (stock.Ticker.ToLower().Contains(msg.searchParam.ToLower()))
                        {
                            var thisTickerDist = Util.LevenshteinDistance.Compute(msg.searchParam.ToLower(), stock.Ticker.ToLower());
                            //_log.Debug($"Ticker distance is {thisTickerDist}");
                            if (bestTickerCandidates.Count < 3)
                                bestTickerCandidates.Add(Tuple.Create<Stock, int>(stock, thisTickerDist));
                            else
                            {
                                var weakest = bestTickerCandidates.OrderByDescending(c => c.Item2).First();
                                if (weakest.Item2 > thisTickerDist)
                                {
                                    //_log.Debug($"Replacing ticker {weakest.Item1.Ticker} with {stock.Ticker}");
                                    bestTickerCandidates.Remove(weakest);
                                    bestTickerCandidates.Add(Tuple.Create<Stock, int>(stock, thisTickerDist));
                                }
                            }
                        }
                        if (stock.Name.ToLower().Contains(msg.searchParam.ToLower()))
                        {
                            var thisNameDist = Util.LevenshteinDistance.Compute(msg.searchParam.ToLower(), stock.Name.ToLower());
                            //_log.Debug($"Name distance is {thisNameDist}");
                            if (bestNameCandidates.Count < 3)
                                bestNameCandidates.Add(Tuple.Create<Stock, int>(stock, thisNameDist));
                            else
                            {
                                var weakest = bestNameCandidates.OrderByDescending(c => c.Item2).First();
                                if (weakest.Item2 > thisNameDist)
                                {
                                    bestNameCandidates.Remove(weakest);
                                    bestNameCandidates.Add(Tuple.Create<Stock, int>(stock, thisNameDist));
                                }
                            }
                        }
                    }
                    var res = bestTickerCandidates.Concat(bestNameCandidates).DistinctBy(c => c.Item1.Id).Select(s => s.Item1.ToDTO());
                    var result = new DbOperationResult<List<StockDTO>>(res.ToList());
                    Sender.Tell(result);
                }
            });

            Receive<StockMessage.UpdateUsCCC>(msg =>
            {
                //Sender.Tell(UpdateUSCCC(msg.filewithPath));
                throw new NotImplementedException("This feature has been removed, please use the one in StockService");
            });

            /*Receive<StockMessage.UpdateRelevantStockPrices>(msg =>
            {
                _log.Debug("Received UpdateRelevantStockPrices - starting task...");
                var report = msg.reportBack;
                var sender = Sender;
                //UpdateRelevantStockPricesFromAlphaVantage().ContinueWith(task =>

                var now = DateTime.UtcNow;
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                {
                    _log.Debug("Not a trading day, skipping updates");
                    sender.Tell(StockMessage.NewUpdateRelevantStockPricesDone(true));
                    return;
                }

                if (now.Hour >= 14 && now.Hour <= 21) //Todo: kesä/talviaika jne. 
                {
                    UpdateRelevantStockPricesFromIEX().ContinueWith(task =>
                    {
                        _log.Debug("Task finished, reporting back = " + report);
                        if (report)
                        {
                            if (task.IsCompletedSuccessfully)
                                sender.Tell(StockMessage.NewUpdateRelevantStockPricesDone(true));
                            else
                                sender.Tell(StockMessage.NewUpdateRelevantStockPricesDone(false));
                        }
                    });
                }
                else
                {
                    _log.Debug("This time is outside trading hours, skipping update");
                }
            });*/

            Receive<StockMessage.UpdateRelevantIexStockPrices>(msg =>
            {
                _log.Debug("Received UpdateRelevantIexStockPrices, starting UpdateRelevantStockPricesFromIEX() Task...");
                UpdateRelevantStockPricesFromIEX(msg.UpdateId).ContinueWith(task =>
                {
                    _log.Debug("UpdateRelevantStockPricesFromIEX() Task finished");
                });
            });

            Receive<StockMessage.UpdateAllIexStockPrices>(msg =>
            {
                _log.Debug("Received UpdateAllIexStockPrices, starting UpdateAllStockPricesFromIEX() Task...");
                UpdateAllStockPricesFromIEX(msg.UpdateId).ContinueWith(task =>
                {
                    _log.Debug("UpdateAllStockPricesFromIEX() Task finished");
                });
            });
        }

        /*private bool UpdateUSCCC(string fileWithPath)
        {
            _log.Debug("Starting to process US CCC excel update...");
            bool allOk = true;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                const int EKARIVI = 6; //Eka varsinainen datarivi otsikkojen yms sälän jälkeen. Alkaa nollasta nyt!
                int updates = 0;
                int additions = 0;
                int removed = 0;

                System.IO.StreamReader file = new System.IO.StreamReader(fileWithPath);
                FileInfo newFile = new FileInfo(fileWithPath);
                var inputStream = new FileStream(fileWithPath, FileMode.Open, FileAccess.Read);
                IWorkbook workbook = WorkbookFactory.Create(inputStream);
                //if (workbook is XSSFWorkbook)
                //{
                //    XSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook); //To actually have formulas as values
                //}
                //else
                //{
                //    HSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook);
                //}

                ISheet cccSheet = workbook.GetSheet("All CCC");
                ISheet champSheet = workbook.GetSheet("Champions");

                var fileUpdated = champSheet.GetRow(4).GetCell(7).DateCellValue;

                List<Stock> NewStockData = new List<Stock>();

                for (int row = EKARIVI; row <= cccSheet.LastRowNum; row++)
                {
                    bool failed = false;
                    Stock s = new Stock();
                    s.Currency = Currency.USD;
                    s.DataLastUpdated = fileUpdated; 
                    s.PriceLastUpdated = fileUpdated;
                    var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

                    try
                    {
                        var firstCellInRow = cccSheet.GetRow(row).GetCell(0);
                        if (firstCellInRow == null || firstCellInRow.StringCellValue == null || firstCellInRow.StringCellValue == "")
                            break;
                        s.Name = firstCellInRow.StringCellValue;
                        if (s.Name.ToLower().StartsWith("averages for"))
                            break;
                        s.Ticker = cccSheet.GetRow(row).GetCell(1).StringCellValue;
                        s.Sector = cccSheet.GetRow(row).GetCell(2).StringCellValue;
                        s.DGYears = (int)Math.Ceiling(cccSheet.GetRow(row).GetCell(4).NumericCellValue);
                        s.CurrentPrice = cccSheet.GetRow(row).GetCell(8).NumericCellValue;
                        s.Dividend = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(12), evaluator);
                        s.EpsTtm = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(28));
                        s.EpsGrowth5 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(35)); 
                        s.DeptToEquity = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(39));
                        //s.DivGrowth1 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(37));
                        //s.DivGrowth3 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(38));
                        //s.DivGrowth5 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(39));
                        //s.DivGrowth10 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(40));
                        s.PriceSource = PriceSource.IEXCloud;
                        s.DataUpdateSource = DataUpdateSource.US_CCC;
                        s.Exchange = Exchange.NyseNasdaq;
                    }
                    catch (Exception e)
                    {
                        _log.Error("Invalid datafield while reading stock row: " + row + ". Problem: " + e.Message);
                        failed = true;
                        allOk = false;
                    }
                    if (failed)
                    {
                        _log.Error("Invalid data in row: " + row);
                        break;
                    }

                    if (!failed)
                    {
                        NewStockData.Add(s);
                    }
                }

                try
                {
                    StringBuilder sbAdd = new StringBuilder();
                    sbAdd.AppendLine("Added the following stocks to database as new US CCC stocks:");
                    foreach (var s in NewStockData)
                    {
                        var dbstock = ctx.Stocks.FirstOrDefault(ds => ds.Ticker == s.Ticker);
                        if (dbstock != null)
                        {
                            if (fileUpdated > dbstock.DataLastUpdated)
                            {
                                dbstock.DGYears = s.DGYears;
                                dbstock.Dividend = s.Dividend;
                                dbstock.EpsTtm = s.EpsTtm;
                                dbstock.EpsGrowth5 = s.EpsGrowth5;
                                dbstock.DivGrowth1 = s.DivGrowth1;
                                dbstock.DivGrowth3 = s.DivGrowth3;
                                dbstock.DivGrowth5 = s.DivGrowth5;
                                dbstock.DivGrowth10 = s.DivGrowth10;
                                dbstock.DeptToEquity = s.DeptToEquity;
                                dbstock.DataLastUpdated = fileUpdated;
                                if (dbstock.PriceLastUpdated < fileUpdated)
                                {
                                    dbstock.CurrentPrice = s.CurrentPrice;
                                    dbstock.PriceLastUpdated = fileUpdated;
                                }
                                updates++;
                            }
                        }
                        else if (dbstock == null)
                        {
                            sbAdd.AppendLine(s.Ticker + " (" + s.Name + ")");
                            ctx.Stocks.Add(s);
                            additions++;
                        }
                    }
                    _log.Debug(sbAdd.ToString());

                    //Update stocks that have datasource as US_CCC but are not in new excel
                    var missingInFile = ctx.Stocks.Where(ss => ss.DataUpdateSource == DataUpdateSource.US_CCC).ToList().Where(st => !NewStockData.Any(n => n.Ticker == st.Ticker)).Select(t => t.Ticker).ToList();

                    StringBuilder sbDel = new StringBuilder();
                    sbDel.AppendLine("Deleted the following stocks from database entirely due to them being no longer in US CCC list:");
                    StringBuilder sbDown = new StringBuilder();
                    sbDown.AppendLine("Downgraded the following stocks to manual update due to them no longer being in US CCC list:");
                    foreach (var ticker in missingInFile)
                    {
                        var missing = ctx.Stocks.FirstOrDefault(sto => sto.Ticker == ticker);

                        var existsInWatchlist = ctx.WatchlistStocks.FirstOrDefault(w => w.Stock.Ticker == ticker) != null;
                        var existsInPortfolio = ctx.PortfolioPositions.FirstOrDefault(p => p.Stock.Ticker == ticker) != null;

                        //If stock is not in any "use", remove entirely
                        if (!existsInWatchlist && !existsInPortfolio)
                        {
                            //_log.Debug("Deleting stock " + missing.Ticker + " from database entirely due to it being no longer in US CCC list.");
                            sbDel.AppendLine(missing.Ticker + " (" + missing.Name + ")");
                            ctx.Stocks.Remove(missing);
                        }
                        else
                        {
                            //_log.Debug("Downgrading stock " + missing.Ticker + " to manual update due to it being no longer in US CCC list.");
                            sbDown.AppendLine(missing.Ticker + " (" + missing.Name + ")");
                            missing.DataUpdateSource = DataUpdateSource.Manual;
                            missing.DGYears = 0; //Ettei ainakaan jää vanha data!
                            //TODO: Kyllä kai Stockiin voisi lisätä kentän "RemoveFromCCC" tms, olisi selvempi
                        }
                        removed++;
                    }
                    _log.Debug(sbDown.ToString());
                    _log.Debug(sbDel.ToString());
                }
                catch (Exception e)
                {
                    _log.Error("Failure updating stocks in database. Problem: " + e.Message);
                    allOk = false;
                }

                file.Close();
                file.Dispose();

                ctx.SaveChanges();

                _log.Debug("US CCC update done. Updated " + updates + ", added " + additions + " and stripped from ccc " + removed + " stocks in database");
            }
            return allOk;
        }*/


        /*private Task UpdateRelevantStockPricesFromAlphaVantage()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();

                var stocksToUpdate = ctx.Stocks.Where(s => s.PriceSource == PriceSource.AlphaVantage)
                    .Where(s => ctx.WatchlistStocks.Any(wls => wls.Stock.Id == s.Id) || s.DGYears >= 25);
                var ok = UpdateStockPricesFromAlphaVantage(ctx, stocksToUpdate);
                if (!ok)
                    return Task.FromException(new InvalidOperationException("Updating stock prices failed"));
            }

            return Task.CompletedTask;
        }*/

        private async Task UpdateRelevantStockPricesFromIEX(int updateId)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();

                //Update those on any watchlist, on any portfolio, or with more than 25 years of div growth
                var stocksToUpdate = ctx.Stocks.Where(s => s.Exchange == Exchange.NyseNasdaq)
                    //.Where(s => ctx.WatchlistStocks.Any(wls => wls.Stock.Id == s.Id) || s.DGYears >= 25 || ctx.PortfolioPositions.Any(pp => pp.Stock.Id == s.Id));
                    .Where(s => ctx.Watchlists.SelectMany(wl => wl.WatchlistStocks).Any(wls => wls.Stock.Id == s.Id) || s.DGYears >= 25 || ctx.Portfolios.Any(p => p.Positions.Any(pp => pp.Stock.Id == s.Id)));
                var ok = await UpdateStockPricesFromIex(ctx, stocksToUpdate, updateId);
                if (!ok)
                    throw new InvalidOperationException("Updating stock prices from IEX failed");
            }

            return;
        }

        private async Task UpdateAllStockPricesFromIEX(int updateId)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();

                //Update those on any watchlist, on any portfolio, or with more than 25 years of div growth
                var stocksToUpdate = ctx.Stocks.Where(s => s.Exchange == Exchange.NyseNasdaq);
                var ok = await UpdateStockPricesFromIex(ctx, stocksToUpdate, updateId);
                if (!ok)
                    throw new InvalidOperationException("Updating stock prices from IEX failed");
            }

            return;
        }

        /*private bool UpdateStockPricesFromAlphaVantage(FinanceManagerContext ctx, IEnumerable<Stock> stocksToUpdate)
        {
            int MAXPERQUERY = 90;
            List<Stock> DBStocks = stocksToUpdate.ToList();
            List<string> UpdatedTickers = new List<string>();
            List<double> NewPrices = new List<double>();
            _log.Debug($"Updating stock prices from AlphaVantage for {DBStocks.Count} stocks...");

            if (DBStocks.Count == 0)
                return true;

            _log.Debug("Getting AlphaVantage API key...");
//            
//#if DEBUG
//            var apiKey = System.Environment.GetEnvironmentVariable("ALPHAVANTAGE_APIKEY", EnvironmentVariableTarget.User);
//#else
//            var apiKey = System.Environment.GetEnvironmentVariable("ALPHAVANTAGE_APIKEY"); //Azure
//#endif        

            var apiKey = System.Environment.GetEnvironmentVariable("ALPHAVANTAGE_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve AlphaVantage apiKey from Environment settings. Exiting update.");
                return false;
            }
            _log.Debug("Found AlphaVantage API key: " + apiKey);

            int ylaraja = MAXPERQUERY;
            int alaraja = 0;
            bool allUpdated = false;
            int requestNumber = 1;
            while (!allUpdated)
            {
                if (ylaraja > DBStocks.Count)
                {
                    ylaraja = DBStocks.Count;
                    allUpdated = true; //seuraavalla kierroksella ei enää jatketa
                }

                List<string> replaced_tickers = new List<string>();
                string tickerRow = "";

                for (int i = alaraja; i < ylaraja; i++)
                {
                    if (DBStocks[i].PriceSource != PriceSource.AlphaVantage)
                        continue;

                    string tick = DBStocks[i].Ticker;
                    //muutetaan tickerissä mahdolliseti oleva viiva pisteeksi niin haku toimii
                    int pos = tick.IndexOf("-");
                    if (pos != -1 && DBStocks[i].Currency == Currency.USD) // Currency temp ratkaisu, esim Munich Re:n (EUR) tickerin MuV2.DE ja TD.TO:n (CAD) pistettä ei korvata
                    {
                        tick = tick.Replace("-", ".");
                        replaced_tickers.Add(tick); // Pistemuoto muokattujen listaan. Nyt siis esim BWL-A on listassa BWL.A muodossa. 
                    }
                    tickerRow = tickerRow + tick + ",";
                }
                tickerRow = tickerRow.Substring(0, tickerRow.Length - 1); // jätetään viimeinen pilkku pois

                var now = DateTime.UtcNow;

                if (AlphaVantageCallTimes.Count > 5)
                {
                    var oldestCall = AlphaVantageCallTimes.OrderByDescending(t => t).Last();

                    while(oldestCall.AddSeconds(61) >= now)
                    {                       
                        _log.Debug($"It's now {DateTime.UtcNow.ToShortTimeString()}, oldest AlphaVantage call was {oldestCall.ToShortTimeString()} - waiting...");
                        Task.Delay(1000).Wait();
                        oldestCall = AlphaVantageCallTimes.OrderByDescending(t => t).Last();
                    }
                       
                    AlphaVantageCallTimes.Remove(oldestCall);
                    AlphaVantageCallTimes.Add(DateTime.UtcNow);                   
                }
                else
                    AlphaVantageCallTimes.Add(DateTime.UtcNow);

                var query = $"https://www.alphavantage.co/query?function=BATCH_STOCK_QUOTES&symbols={tickerRow}&apikey={apiKey}&datatype=csv";
                WebRequest request = WebRequest.Create(query);
                _log.Debug($"Requesting data #{requestNumber} from AlphaVantage with query: {query}");


                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);

                //var path = HttpRuntime.AppDomainAppPath;
                var path = AppDomain.CurrentDomain.BaseDirectory;
                System.IO.StreamWriter fileOut = new System.IO.StreamWriter(path + "AlphaVantage-response" + requestNumber + ".dat");
                reader.ReadLine(); //Skip title row

                int updateCount = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    fileOut.WriteLine(line);
                    if (line == null || line.Length < 1)
                        break;

                    string[] fields = StringRoutines.SplitLine(line, ',');
                    string ticker = fields[0].Replace('"', ' ').Trim();

                    int pos = ticker.IndexOf(".");
                    if (pos != -1)
                    {
                        if (replaced_tickers.IndexOf(ticker) != -1) //Jos tässä tickerissä on piste ".", katsotaan onko se seurausta korvauksesta edellä
                            ticker = ticker.Replace(".", "-"); //Edellä muutettiin viivat pisteiksi, muutetaan takaisin
                    }
                    //Onko tietokannassa tätä osaketta? Jos ei, skipataan päivitys
                    if (DBStocks.FirstOrDefault(st => st.Ticker == ticker) == null)
                    {
                        _log.Error($"AlphaVantage API returned ticker '{ticker}' which is not recognized as stock in our database - skipping");
                        continue;
                    }

                    Stock s = DBStocks.FirstOrDefault(st => st.Ticker == ticker);
                    _log.Debug("AlphaVantagesta saatu hinta osakkeelle " + s.Ticker + ": " + fields[1] + ", yritetään konvertoida...");
                    try
                    {
                        s.UpdatePrice(double.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture), DataUpdateSource.AlphaVantage);
                        NewPrices.Add(double.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture));
                        UpdatedTickers.Add(ticker);
                    }
                    catch (System.FormatException e)
                    {
                        _log.Error("Cannot convert value " + fields[1] + " to double:" + e.Message);
                    }

                    var current = ctx.Stocks.FirstOrDefault(st => st.Ticker == ticker);
                    current.UpdatePrice(s.CurrentPrice);
                    updateCount++;

                }
                fileOut.Close();
                _log.Debug(updateCount + " prices updated!");

                reader.Close();
                response.Close();

                alaraja = ylaraja;
                ylaraja += MAXPERQUERY;
                requestNumber++;
            }
            if (NewPrices.Count(price => price < 0.01) < 0.05 * NewPrices.Count) //Trick for skipping saving all-zeros from Alpha-Vantage: Less than 5% can be 0
                ctx.SaveChanges();
            else
                _log.Debug("AlphaVantage reply had too many prices as zero, not saving changes to database");

            //Lokitetan tieto epäonnistuneista
            var notUpdated = DBStocks.Where(s => !UpdatedTickers.Any(u => u == s.Ticker));
            if (notUpdated.Count() > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"Failed to get updated price for the following {notUpdated.Count()} stocks: \n");
                foreach (var fail in notUpdated)
                {
                    sb.Append(fail.Ticker + ", ");
                }
                sb.Append('\n');
                _log.Error(sb.ToString());
            }

            return true;
        }*/





        private async Task<bool> UpdateStockPricesFromIex(FinanceManagerContext ctx, IEnumerable<Stock> stocksToUpdate, int updateId)
        {
            int MAXPERQUERY = 95;
            List<Stock> DBStocks = stocksToUpdate.ToList();
            List<string> UpdatedTickers = new List<string>();
            List<double> NewPrices = new List<double>();
            _log.Debug($"Updating stock prices from IEXCloud for {DBStocks.Count} stocks...");

            if (DBStocks.Count == 0)
                return true;

            _log.Debug("Getting IEXCloud API key...");

            var apiKey = System.Environment.GetEnvironmentVariable("IEXCLOUD_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve IEXCloud apiKey from Environment settings. Exiting update.");
                return false;
            }
            _log.Debug("Found IEXCloud API key: " + apiKey);

            int ylaraja = MAXPERQUERY;
            int alaraja = 0;
            bool allUpdated = false;
            int requestNumber = 1;
            int creditsUsed = 0;
            var client = new HttpClient();
            while (!allUpdated)
            {
                if (ylaraja > DBStocks.Count)
                {
                    ylaraja = DBStocks.Count;
                    allUpdated = true; //seuraavalla kierroksella ei enää jatketa
                }

                List<string> replaced_tickers = new List<string>();
                string tickerRow = "";

                for (int i = alaraja; i < ylaraja; i++)
                {
                    string tick = DBStocks[i].Ticker;
                    //muutetaan tickerissä mahdolliseti oleva viiva pisteeksi niin haku toimii
                    int pos = tick.IndexOf("-");
                    if (pos != -1 && DBStocks[i].Currency == Currency.USD) // Currency temp ratkaisu, esim Munich Re:n (EUR) tickerin MuV2.DE ja TD.TO:n (CAD) pistettä ei korvata
                    {
                        tick = tick.Replace("-", ".");
                        replaced_tickers.Add(tick); // Pistemuoto muokattujen listaan. Nyt siis esim BWL-A on listassa BWL.A muodossa. 
                    }
                    tickerRow = tickerRow + tick + ",";
                }
                tickerRow = tickerRow.Substring(0, tickerRow.Length - 1); // jätetään viimeinen pilkku pois

                await Task.Delay(50);

                var path = AppDomain.CurrentDomain.BaseDirectory;
                StreamWriter fileOut = new System.IO.StreamWriter(path + "IEXCloud-response" + requestNumber + ".json");

                var request = new HttpRequestMessage
                {
                    //RequestUri = new Uri($"https://sandbox.iexapis.com/stable/stock/market/batch?&symbols={tickerRow}&types=quote&token={apiKey}"),
                    RequestUri = new Uri($"https://cloud.iexapis.com/stable/stock/market/batch?&symbols={tickerRow}&types=quote&token={apiKey}"),
                    Method = HttpMethod.Get,
                };

                var response = await client.SendAsync(request);

                _log.Debug("Iex response:");
                var sb = new StringBuilder();
                sb.AppendLine("\nHeaders:");
                sb.AppendLine($"Content-Type: {response.Content.Headers.ContentType}");
                foreach (var h in response.Headers)
                {
                    sb.AppendLine($"{h.Key}: {h.Value?.FirstOrDefault()}");
                }
                sb.AppendLine("Content:");
                var jsonString = await response.Content.ReadAsStringAsync();
                sb.AppendLine(jsonString);
                _log.Debug(sb.ToString());

                var creditsStr = response.Headers.FirstOrDefault(h => h.Key == "iexcloud-messages-used").Value?.FirstOrDefault();
                if (!string.IsNullOrEmpty(creditsStr))
                {
                    var creditsOk = int.TryParse(creditsStr, out var creditsUsedInThisQuery);
                    if (creditsOk)
                        creditsUsed += creditsUsedInThisQuery;
                    else
                        _log.Error($"Unable to parse credits ued from header value: {creditsStr}");
                }
                else
                {
                    _log.Error($"Unable to get value for iexcloud-messages-used header, cannot update credit use");
                }

                if (response.IsSuccessStatusCode)
                {                  
                    fileOut.WriteLine(jsonString);
                    try
                    {
                        _log.Debug("Deserializing json...");
                        //var camelCaseSerializerSettings = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        //var quotes = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Quote>>>(jsonString);
                        var quotes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Quote>>>(jsonString);
                        foreach (var quote in quotes)
                        {
                            var tick = quote.Value["quote"].Symbol;
                            int pos = tick.IndexOf(".");
                            if (pos != -1)
                            {
                                if (replaced_tickers.IndexOf(tick) != -1) //Jos tässä tickerissä on piste ".", katsotaan onko se seurausta korvauksesta edellä
                                    tick = tick.Replace(".", "-"); //Edellä muutettiin viivat pisteiksi, muutetaan takaisin
                            }

                            Stock s = ctx.Stocks.FirstOrDefault(st => st.Ticker == tick);
                            if (s == null)
                            {
                                _log.Error($"IEXCloud API returned ticker '{tick}' which is not recognized as stock in our database - skipping");
                                continue;
                            }
                            
                            _log.Debug($"IEXCloudista saatu hinta osakkeelle {s.Ticker} :  {quote.Value["quote"].LatestPrice} ...");
                            if (quote.Value["quote"].LatestPrice.HasValue) //Update only if we got value - otherwise this stock will be end up in the failed list
                            {
                                s.UpdatePrice(quote.Value["quote"].LatestPrice.Value, DataUpdateSource.IEXCloud, ctx);
                                //NewPrices.Add(double.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture));
                                UpdatedTickers.Add(tick);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Failed to process iex response: {e.Message}");
                    }
                }
                else
                {
                   
                }
                fileOut.Flush();
                fileOut.Close();             

                alaraja = ylaraja;
                ylaraja += MAXPERQUERY;
                requestNumber++;
            }

            ctx.SaveChanges();

            //Log the failed ones, if any
            var notUpdated = DBStocks.Where(s => !UpdatedTickers.Any(u => u == s.Ticker));
            if (notUpdated.Count() > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"Failed to get updated IEXCloud price for the following {notUpdated.Count()} stocks: \n");
                foreach (var fail in notUpdated)
                {
                    sb.Append(fail.Ticker + ", ");
                }
                sb.Append('\n');
                _log.Error(sb.ToString());
            }
           
            var update = ctx.IexUpdateRuns.FirstOrDefault(s => s.Id == updateId);
            if (update != null)
            {
                _log.Debug($"IEX price update done for {UpdatedTickers.Count} stocks, adding {creditsUsed} to CreditsUsed");
                update.CreditsUsed += creditsUsed;
                ctx.SaveChanges();
            }
            else
            {
                _log.Error($"Failed to get IexUpdateRun with id {updateId} from database, cannot update credits usage");
                return true;
            }

            return true;
        }


    }
}
