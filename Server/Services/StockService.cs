using NLog;
using Server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using System.Net.Http;
using System.Text.Json;
using Server.Models;
using Server.Services.StockServices;
using Microsoft.EntityFrameworkCore;
using Common.HelperModels;
using NPOI.SS.UserModel;
using System.Text;
using Server.Util;
using System.IO;
using Common.Dtos;
using Server.Mappings;
using FinanceManager.Server.Database;
using Financemanager.Server.Database.Domain;

namespace Server.Services
{
    public class StockService: IDisposable
    {
        private readonly FinanceManagerContext _ctx;
        private Logger _log;
        public readonly int InstanceNumber;
        private static int InstancesCreated;
        private List<DateTime> AlphaVantageRequestTimes = new List<DateTime>();
        private HttpClient _client;

        public StockService(FinanceManagerContext ctx)
        {
            _log = LogManager.GetCurrentClassLogger();
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);
            _log.Debug($"StockService #{InstanceNumber} created with FinanceManagerContext instance #{ctx.InstanceNumber}");
            _ctx = ctx;
            _client = new HttpClient(); //TODO: DI?
        }

        // Updates plan: 
        // US-CCC stocks: Price from iex, data ccc-list
        // Finnish, Amsterdam, german, canadian stocks: Price some RapidApi free plan, basic data (dividend, eps at least) RapidApiStockDataYFAlt
        // Dividend, price history for all: FMP
        // todo: canadian ccc? And need admin-system to upload us-ccc updates.


        //Note: This doesnt seem to be realtime or even 15-min delay, but last close for canadian stocks at least
        /*public async Task UpdateAlphavantagePrices()
        {
            var apiKey = System.Environment.GetEnvironmentVariable("ALPHAVANTAGE_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve AlphaVantage apiKey from Environment settings. Exiting update.");
                throw new NullReferenceException("AlphaVantage API key not found");
            }

            var stocksToUpdate = _ctx.Stocks.Where(s => s.PriceSource == PriceSource.AlphaVantage).Select(s => s.Ticker).ToList();
            Dictionary<string, double> receivedPrices = new Dictionary<string, double>();
            foreach (var ticker in stocksToUpdate)
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={apiKey}"),
                    Method = HttpMethod.Get,
                };

                await RequestAlphaVantageQuery(); //AlphaVantage usage limit 5 queries per minute, 500 per day (latter still not accounted for)

                try
                {
                    var response = await _client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        _log.Debug($"Alphavantage reply for quote query for {ticker}: \n" + jsonString);
                        var quote = JsonSerializer.Deserialize<AlphaVantageStockQuoteResponse>(jsonString);
                        var price = double.Parse(quote.GlobalQuote._05Price, System.Globalization.CultureInfo.InvariantCulture);
                        receivedPrices.Add(ticker, price);
                    }
                    else
                    {
                        _log.Error($"Failed to query AlphaVantage price for {ticker}: {response.ReasonPhrase}");
                    }
                }
                catch(Exception e)
                {
                    _log.Error($"Exception when executing AlphaVantage query: {e} - {e.Message}");
                }
            }

            var updated = 0;
            var missing = 0;

            foreach (var s in stocksToUpdate)
            {
                var stock = _ctx.Stocks.FirstOrDefault(dbs => dbs.Ticker == s);
                if (stock != null)
                {
                    if (receivedPrices.ContainsKey(s))
                    {
                        stock.UpdatePrice(receivedPrices[s], DataUpdateSource.AlphaVantage);
                        updated++;
                    }
                    else
                        missing++;
                }
                else
                    _log.Error($"Cannot update price as somehow database no longer has stock with ticker {s}"); //likely deleted during update since 5+ queries take time
            }
            await _ctx.SaveChangesAsync();
            _log.Debug($"UpdateAlphavantagePrices() tried to update {stocksToUpdate.Count} prices, {updated} succeeded");
        }*/

        //Task that can be waited to stay within allowed request rate. TODO: Mutex/Locks if this is going to be used from multiple places
        private async Task RequestAlphaVantageQuery()
        {
            await Task.Delay(500); //Always 500ms between calls
            if (AlphaVantageRequestTimes.Count < 5)
            {
                AlphaVantageRequestTimes.Add(DateTime.UtcNow);
                return;
            }
            else
            {
                var oldest = AlphaVantageRequestTimes.OrderBy(a => a).First();
                while(oldest > DateTime.UtcNow.AddSeconds(-61))
                {
                    await Task.Delay(500);
                }

                AlphaVantageRequestTimes.Remove(oldest);
                AlphaVantageRequestTimes.Add(DateTime.UtcNow);
            }
        }


        /*public async Task UpdateFMPPrices()
        {
            var apiKey = System.Environment.GetEnvironmentVariable("FMP_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve FMP apiKey from Environment settings. Exiting update.");
                throw new NullReferenceException("FMP API key not found");
            }

            var stocksToUpdate = _ctx.Stocks.Where(s => s.PriceSource == PriceSource.AlphaVantage).Select(s => s.Ticker).ToList();
            Dictionary<string, double> receivedPrices = new Dictionary<string, double>();
            foreach (var ticker in stocksToUpdate)
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://financialmodelingprep.com/api/v3/quote/{ticker}?apikey={apiKey}"),
                    Method = HttpMethod.Get,
                };
                request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

                await RequestAlphaVantageQuery(); //TESTING, using same limit 5 per minute as AlphaVantage

                try
                {
                    var response = await _client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        _log.Debug($"FMP reply for quote query for {ticker}: \n" + jsonString);
                        var quote = JsonSerializer.Deserialize<List<FMPStockQuoteResponse>>(jsonString);
                        var price = quote[0].price;
                        //Täältä saatais myös eps!
                        receivedPrices.Add(ticker, price);
                    }
                    else
                    {
                        _log.Error($"Failed to query FMP price for {ticker}: {response.ReasonPhrase}");
                    }
                }
                catch (Exception e)
                {
                    _log.Error($"Exception when executing FMP query: {e} - {e.Message}");
                }
            }

            var updated = 0;
            var missing = 0;

            foreach (var s in stocksToUpdate)
            {
                var stock = _ctx.Stocks.FirstOrDefault(dbs => dbs.Ticker == s);
                if (stock != null)
                {
                    if (receivedPrices.ContainsKey(s))
                    {
                        stock.UpdatePrice(receivedPrices[s]);
                        updated++;
                    }
                    else
                        missing++;
                }
                else
                    _log.Error($"Cannot update price as somehow database no longer has stock with ticker {s}"); //likely deleted during update since 5+ queries take time
            }
            await _ctx.SaveChangesAsync();
            _log.Debug($"UpdateFMPPrices() tried to update {stocksToUpdate.Count} prices, {updated} succeeded");
        }*/


        public async Task UpdateDividendHistory(string ticker)
        {
            var stock = _ctx.Stocks.FirstOrDefault(s => s.Ticker == ticker);
            if (stock == null)
            {
                _log.Error($"Handling dividend history reply for ticker {ticker} which odesnt exist as stock in database");
                return;
            }


            await RequestAlphaVantageQuery(); //TESTING, using same limit 5 per minute as AlphaVantage

            FMPDividendHistoryResponse resp = new FMPDividendHistoryResponse() { historical = new List<HistoricalDiv>() };
            try
            {
                using (var fmpClient = new FMPClient(_client))
                {
                    resp = await fmpClient.FetchDividendHistory(ticker);
                }
            }
            catch(Exception e)
            {
                _log.Error($"FmpClient was unable to fetch dividend history for {ticker}: {e} - {e.Message}");
                return;
            }
              

            //TODO: Katso onko jo kannassa, tallenna (tai korvaa jos muuttunut?) jos ei
            var thisStockHist = _ctx.HistoricalDividend.Where(h => h.StockId == stock.Id).ToList();
            var updated = 0;
            var added = 0;

            foreach (var div in resp.historical)
            {
                var divDate = DateTime.Parse(div.date);
                var dayMatch = thisStockHist.FirstOrDefault(d => d.ExDividendDate == divDate);
                if (dayMatch != null && dayMatch.AmountPerShare == div.adjDividend)
                {
                    _log.Debug($"{ticker} dividend paid {div.date} already exists with same amount {div.adjDividend} - no need for updates");
                }
                else if (dayMatch != null && dayMatch.AmountPerShare != div.adjDividend)
                {
                    _log.Debug($"{ticker} dividend {div.adjDividend} paid {div.date} already exists with different amount {dayMatch.AmountPerShare} - updating");
                    dayMatch.AmountPerShare = div.adjDividend;
                    updated++;
                }
                else if (dayMatch == null)
                {
                    _log.Debug($"{ticker} dividend {div.adjDividend} paid {div.date} does not exist in database - adding");
                    var newHist = new HistoricalDividend()
                    {
                        StockId = stock.Id,
                        AmountPerShare = div.adjDividend,
                        ExDividendDate = divDate,
                        PaymentdDate = string.IsNullOrEmpty(div.paymentDate) ? null : DateTime.Parse(div.paymentDate),
                    };
                    //thisStockHist.Append(newHist);
                    _ctx.HistoricalDividend.Add(newHist);
                    added++;
                }
            }

            _log.Debug($"UpdateDividendHistory() updated dividend history for stock {ticker}: {added} entries added, {updated} updated");
            _ctx.ApiProviderRuns.Add(new ApiProviderRun() { CreditsUsed = 1, TimestampUtc = DateTime.UtcNow, ProviderName = "FMP" }); //TODO: Maybe not here, but on calling level
            await _ctx.SaveChangesAsync();
        }


        public async Task UpdatePriceHistory(string ticker)
        {
            var stock = _ctx.Stocks.FirstOrDefault(s => s.Ticker == ticker);
            if (stock == null)
            {
                _log.Error($"Requested price history for ticker {ticker} which doesnt exist as stock in database");
                return;
            }


            await RequestAlphaVantageQuery(); //TESTING, using same limit 5 per minute as AlphaVantage

            FMPPriceHistoryResponse resp = new FMPPriceHistoryResponse() { historical = new List<StockHistoricalPrice>() };
            try
            {
                using (var fmpClient = new FMPClient(_client))
                {
                    resp = await fmpClient.FetchPriceHistory(ticker);
                }
            }
            catch (Exception e)
            {
                _log.Error($"FmpClient was unable to fetch price history for {ticker}: {e} - {e.Message}");
                return;
            }


            //TODO: Katso onko jo kannassa, tallenna (tai korvaa jos muuttunut?) jos ei
            var thisStockHist = _ctx.HistoricalPrice.Where(h => h.StockId == stock.Id).ToList();
            var updated = 0;
            var added = 0;

            foreach (var price in resp.historical)
            {
                var date = DateTime.Parse(price.date);
                var dayMatch = thisStockHist.FirstOrDefault(d => d.Date == date);
                if (dayMatch != null && dayMatch.ClosePrice == price.close)
                {
                    _log.Debug($"{ticker} price {price.date} already exists with same amount {price.close} - no need for updates");
                }
                else if (dayMatch != null && dayMatch.ClosePrice != price.close)
                {
                    _log.Debug($"{ticker} price {price.close} paid {price.date} already exists with different amount {dayMatch.ClosePrice} - updating");
                    dayMatch.ClosePrice = price.close;
                    updated++;
                }
                else if (dayMatch == null)
                {
                    _log.Debug($"{ticker} price {price.close} paid {price.date} does not exist in database - adding");
                    var newHist = new HistoricalPrice()
                    {
                        StockId = stock.Id,
                        ClosePriceAdjusted = price.adjClose,
                        ClosePrice = price.close,
                        Date = DateTime.Parse(price.date),
                    };
                    _ctx.HistoricalPrice.Add(newHist);
                    added++;
                }
            }

            _log.Debug($"UpdatePriceHistory() updated price history for stock {ticker}: {added} entries added, {updated} updated");
            _ctx.ApiProviderRuns.Add(new ApiProviderRun() { CreditsUsed = 1, TimestampUtc = DateTime.UtcNow, ProviderName = "FMP" }); //TODO: Maybe not here, but on calling level
            await _ctx.SaveChangesAsync();
        }





        public async Task<int> DoEuropeanPriceUpdates(string updateType)
        {
            _log.Debug($"Starting DoEuropeanPriceUpdates for update {updateType}");
            var now = DateTime.UtcNow;
            var providerName = RapidApiYHFinanceClient.ClientName;
            var creditsUsedThisMonth = _ctx.ApiProviderRuns
                .Where(a => a.ProviderName == providerName && a.TimestampUtc.Month == now.Month && a.TimestampUtc.Year == now.Year).Sum(s => s.CreditsUsed);
            if (creditsUsedThisMonth >= 500)
            {
                _log.Error("Cannot do more requests to RapidApiYHFinance, credits depleted");
            }

            var euroExchanges = new List<Exchange>() { Exchange.Amsterdam, Exchange.Helsinki, Exchange.Copenhagen, Exchange.Frankfurt };
            var rapidApiYHFinanceUpdateable = _ctx.Stocks.Where(s => euroExchanges.Contains(s.Exchange) 
                /*&& s.StockDataUpdateSources.Any(d => d.Source == DataUpdateSource.RapidApiYHFinance && d.Attribute == StockAttribute.Price)*/).Select(s => s.Ticker).ToList();          

            return await DoRapidApiYHFinanceQuoteUpdates(updateType, rapidApiYHFinanceUpdateable);
        }



        public async Task<int> DoNorthAmericaPriceUpdates(string updateType)
        {
            _log.Debug($"Starting DoNorthAmericaPriceUpdates for update {updateType}");
            //TODO: Move IEX update here (separate client etc)
            var now = DateTime.UtcNow;
            var canadaProviderName = RapidApiYHFinanceClient.ClientName;
            var creditsUsedThisMonth = _ctx.ApiProviderRuns
                .Where(a => a.ProviderName == canadaProviderName && a.TimestampUtc.Month == now.Month && a.TimestampUtc.Year == now.Year).Sum(s => s.CreditsUsed);
            if (creditsUsedThisMonth >= 500)
            {
                _log.Error("Cannot do more requests to RapidApiYHFinance, credits depleted");
            }

            var rapidApiYHFinanceUpdateable = _ctx.Stocks.Where(s => s.Exchange == Exchange.TSX
                /*&& s.StockDataUpdateSources.Any(d => d.Source == DataUpdateSource.RapidApiYHFinance && d.Attribute == StockAttribute.Price)*/).Select(s => s.Ticker).ToList();

            return await DoRapidApiYHFinanceQuoteUpdates(updateType, rapidApiYHFinanceUpdateable);
        }

        public async Task<int> DoFmpPriceUpdates(string updateType)
        {
            _log.Debug($"Starting FMP price updates for update {updateType}");

            var now = DateTime.UtcNow;
            var fmpProviderName = FMPClient.ClientName;
            var creditsUsedToday = _ctx.ApiProviderRuns
                .Where(a => a.ProviderName == fmpProviderName && a.TimestampUtc.Month == now.Month && a.TimestampUtc.Year == now.Year).Sum(s => s.CreditsUsed);
            if (creditsUsedToday >= 250)
            {
                _log.Error("Cannot do more requests to FMP, credits depleted");
            }

            var fmpUpdateable = _ctx.Stocks.Where(s => s.Exchange == Exchange.NyseNasdaq) //Free FMP = us-only (for now)
               .Select(s => s.Ticker).ToList();

            /*var allSymbols = File.ReadAllText(@"D:\Temp\fmp-tradeable-symbols.json");
            var symbols = JsonSerializer.Deserialize<List<SymbolInfo>>(allSymbols);

            var failTickers = File.ReadAllText(@"D:\Temp\fmp-failing-query.txt").Split("\r\n");
            foreach (var item in failTickers)
            {
                var symbolMatch = symbols.FirstOrDefault(s => s.symbol == item);
                if (symbolMatch != null)
                {
                    System.Diagnostics.Debug.WriteLine(item + ": " + symbolMatch.exchangeShortName);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(item + ": " + "!!NOT FOUND!!");
                }
            }

            return 0;*/

            return await DoFmpQuoteUpdates(updateType, fmpUpdateable);
        }

        public class SymbolInfo //This can be used with fmp symbol query (to get all tradeable symbols, etc)
        {
            public string symbol { get; set; }
            public string name { get; set; }
            //public double price { get; set; }   //throws exception at least with from-disk data, maybe decimal separator
            public string exchange { get; set; }
            public string exchangeShortName { get; set; }
            public string type { get; set; }
        }

        private async Task<int> DoFmpQuoteUpdates(string updateType, List<string> tickers)
        {
            var providerName = FMPClient.ClientName;
            var results = new List<FMPStockQuoteResponse>();
            var remaining = tickers.ToList();
            int usedRequests = 0;
            var replacedTickers = new Dictionary<string, string>();

            using (var fmpClient = new FMPClient(_client))
            {
                while (remaining.Count > 0)
                {
                    var toUpdateNow = remaining.Take(1000).ToList();
                    remaining.RemoveRange(0, remaining.Count >= 1000 ? 1000 : remaining.Count);

                    var needsReplacements = toUpdateNow.Where(s => s.Contains("."));
                    var fmpTickers = toUpdateNow.Select(t => t.Replace(".", "-")).ToArray();
                    foreach (var item in needsReplacements)
                    {
                        replacedTickers.Add(item, item.Replace(".", "-"));
                    }

                    _log.Debug($"Calling FmpClient.GetMultipleQuotes([{String.Join(",", fmpTickers)}])");
                    try
                    {
                        var quotesReply = await fmpClient.GetMultipleQuotes(fmpTickers);
                        if (quotesReply != null)
                        {
                            results.AddRange(quotesReply);
                        }
                        else
                            _log.Error("FMP Quote response is null");
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Exception getting FmpClient quote reply: {e} - {e.Message}");
                        break;
                    }
                    finally
                    {
                        usedRequests++;
                    }
                    await Task.Delay(500);
                }
            }
            int updates = 0;
            foreach (var reply in results)
            {
                Stock stock = null;
                if (!replacedTickers.Values.Any(v => v == reply.symbol))
                    stock = _ctx.Stocks.Include(s => s.StockDataUpdateSources).FirstOrDefault(s => s.Ticker == reply.symbol);
                else
                {
                    var entry = replacedTickers.FirstOrDefault(t => t.Value == reply.symbol);
                    stock = _ctx.Stocks.Include(s => s.StockDataUpdateSources).FirstOrDefault(s => s.Ticker == entry.Key);
                }
                if (stock == null)
                {
                    _log.Warn($"Received FMP data for stock {reply.symbol} but it doesnt exist in db - skipping");
                    continue;
                }
                //TODO: Need to rewrite the data/price source things, now some are on stock model, some on separate table
                //Making everything use same system might also allow sharing the update code more
                /*var priceDataSource = stock.StockDataSources.FirstOrDefault(d => d.Attribute == StockAttribute.Price && d.Source == DataUpdateSource.Fmp);
                if (priceDataSource != null)
                {
                    
                    priceDataSource.LastUpdated = DateTime.UtcNow;
                }*/
                stock.UpdatePrice(reply.price, DataUpdateSource.FMP, _ctx); //TODO: reply.timestamp is int, prolly unix time. Could use that as a time
                updates++;
            }

            if (usedRequests > 0)
            {
                _ctx.ApiUpdateRuns.Add(new ApiUpdateRun() { TimestampUtc = DateTime.UtcNow, UpdateType = updateType });
                _ctx.ApiProviderRuns.Add(new ApiProviderRun() { CreditsUsed = usedRequests, TimestampUtc = DateTime.UtcNow, ProviderName = providerName });
            }

            await _ctx.SaveChangesAsync();
            return updates;
        }

        private async Task<int> DoRapidApiYHFinanceQuoteUpdates(string updateType, List<string> tickers)
        {
            var providerName = RapidApiYHFinanceClient.ClientName;
            var results = new List<RapidApiYHFinanceResult>();
            var remaining = tickers.ToList();
            int usedRequests = 0;

            using (var yhClient = new RapidApiYHFinanceClient(_client))
            {
                while (remaining.Count > 0)
                {
                    var toUpdateNow = remaining.Take(10).ToList();
                    remaining.RemoveRange(0, remaining.Count >= 10 ? 10 : remaining.Count);
                    _log.Debug($"Calling YHFinanceClient.GetQuotes([{String.Join(",", toUpdateNow)}])");
                    try
                    {
                        var quotesReply = await yhClient.GetQuotes(toUpdateNow.ToArray());
                        if (quotesReply?.quoteResponse?.result != null)
                        {
                            results.AddRange(quotesReply.quoteResponse.result);
                        }
                        else
                            _log.Error("Quote response or result is null");
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Exception getting RapidApiYHFinanceClient quote reply: {e} - {e.Message}");
                        break;
                    }
                    finally
                    {
                        usedRequests++;
                    }
                    await Task.Delay(300);
                }
            }
            int updates = 0;
            foreach (var reply in results)
            {
                var stock = _ctx.Stocks.Include(s => s.StockDataUpdateSources)
                    .FirstOrDefault(s => s.Ticker == reply.symbol
                        /*&& s.StockDataUpdateSources.Any(d => d.Source == DataUpdateSource.RapidApiYHFinance)*/);
                if (stock == null)
                {
                    _log.Warn($"Received RapidApiYHFinance data for stock {reply.symbol} but it either doesnt exist in db or doesnt have this datasouce - skipping");
                    continue;
                }

                stock.UpdatePrice(reply.regularMarketPrice, DataUpdateSource.RapidApiYHFinance, _ctx);                
                stock.UpdateEps(reply.epsTrailingTwelveMonths, DataUpdateSource.RapidApiYHFinance, _ctx);                
                stock.UpdateDividend(reply.dividendRate, DataUpdateSource.RapidApiYHFinance, _ctx);
                
                updates++;
            }

            if (usedRequests > 0)
            {
                _ctx.ApiUpdateRuns.Add(new ApiUpdateRun() { TimestampUtc = DateTime.UtcNow, UpdateType = updateType });
                _ctx.ApiProviderRuns.Add(new ApiProviderRun() { CreditsUsed = usedRequests, TimestampUtc = DateTime.UtcNow, ProviderName = providerName });
            }

            await _ctx.SaveChangesAsync();
            return updates;
        }

        public async Task<int> DoSaturdayDataUpdates(string updateType)
        {
            _log.Debug($"Starting DoNorthAmericaPriceUpdates for update {updateType}");
            //TODO: Move IEX update here (separate client etc)
            var now = DateTime.UtcNow;
            var providerName = RapidApiStockDataYFAlternativeClient.ClientName;
            var creditsUsedThisMonth = _ctx.ApiProviderRuns
                .Where(a => a.ProviderName == providerName && a.TimestampUtc.Month == now.Month && a.TimestampUtc.Year == now.Year).Sum(s => s.CreditsUsed);
            if (creditsUsedThisMonth >= 1000) //TODO: dont hardcode...
            {
                _log.Error("Cannot do more requests to RapidApiStockDataYFAlternative, credits depleted");
            }

            var updateable = _ctx.Stocks.Where(s => s.StockDataUpdateSources.Any(d => d.Source == DataUpdateSource.RapidApiStockDataYFAlternative)).Select(s => s.Ticker).ToList(); //TODO: No longer good to select like this

            var results = new List<RapidApiStockDataYFAlternativeDetailsResult>();
            var remaining = updateable.ToList();
            int usedRequests = 0;


            while (remaining.Count > 0)
            {
                var toUpdateNow = remaining.First();
                remaining.RemoveRange(0, 1);

                try
                {
                    using (var raClient = new RapidApiStockDataYFAlternativeClient(_client))
                    {
                        _log.Debug($"Calling raClient.GetDetails({toUpdateNow})");
                        var detailReply = await raClient.GetDetails(toUpdateNow);
                        usedRequests++;
                        if (detailReply?.quoteSummary?.result[0] != null)
                        {
                            results.Add(detailReply.quoteSummary.result[0]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to get RapidApiStockDataYFAlternative response: {ex} - {ex.Message}");
                    break; //Unlikely the remaining requests would work if one fails completely
                }

                await Task.Delay(300); //Limit 5 requests per second, so this will be enough for sure
            }

            int updates = 0;
            foreach (var reply in results)
            {
                var stock = _ctx.Stocks.Include(s => s.StockDataUpdateSources)
                    .FirstOrDefault(s => s.Ticker == reply.price.symbol && s.StockDataUpdateSources.Any(d => d.Source == DataUpdateSource.RapidApiStockDataYFAlternative));
                if (stock == null)
                {
                    _log.Warn($"Received RapidApiStockDataYFAlternative data for stock {reply.price.symbol} but it either doesnt exist in db or doesnt have this datasouce - skipping");
                    continue;
                }

                stock.UpdatePrice(reply.price.regularMarketPrice.raw, DataUpdateSource.RapidApiStockDataYFAlternative, _ctx);
                stock.UpdateDividend(reply.summaryDetail.dividendRate.raw, DataUpdateSource.RapidApiStockDataYFAlternative, _ctx);
                stock.UpdateEps(reply.defaultKeyStatistics.trailingEps.raw, DataUpdateSource.RapidApiStockDataYFAlternative, _ctx);
                updates++;
            }



            if (usedRequests > 0)
            {
                _ctx.ApiUpdateRuns.Add(new ApiUpdateRun() { TimestampUtc = DateTime.UtcNow, UpdateType = updateType });
                _ctx.ApiProviderRuns.Add(new ApiProviderRun() { CreditsUsed = usedRequests, TimestampUtc = DateTime.UtcNow, ProviderName = providerName });
            }
            return updates;
        }

        private async Task<int> DoRapidApiStockDataYFAlternativeDataUpdates(string updateType)
        {
            var stocksToUpdate = _ctx.Stocks.Where(s => s.StockDataUpdateSources.Any(d => d.Source == DataUpdateSource.RapidApiStockDataYFAlternative))
                .Select(s => s.Ticker).ToList();
            int usedRequests = 0;
            _log.Debug($"Starting DoRapidApiStockDataYFAlternativeDataUpdates for {stocksToUpdate.Count} stocks");
            //TODO: miten erotetaan mikä data haetaan mistäkin paikasta? div, eps, growth ratet, jne... lisää flageja/tietoja osakkeelle? jokin lisätaulu?

            var leftToUpdate = stocksToUpdate.ToList();
            List<RapidApiStockDataYFAlternativeDetailsResult> detailResults = new List<RapidApiStockDataYFAlternativeDetailsResult>();

            
            while (leftToUpdate.Count > 0)
            {
                var toUpdateNow = leftToUpdate.First();
                leftToUpdate.RemoveRange(0, 1);

                try
                {
                    using (var raClient = new RapidApiStockDataYFAlternativeClient(_client))
                    {
                        _log.Debug($"Calling raClient.GetDetails({toUpdateNow})");
                        var detailReply = await raClient.GetDetails(toUpdateNow);
                        usedRequests++;
                        if (detailReply?.quoteSummary?.result[0] != null)
                        {
                            detailResults.Add(detailReply.quoteSummary.result[0]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to get RapidApiStockDataYFAlternative response: {ex} - {ex.Message}");
                    break; //Unlikely the remaining requests would work if one fails completely
                }

                await Task.Delay(300); //Limit 5 requests per second, so this will be enough for sure
            }

            foreach (var reply in detailResults)
            {
                //TODO: This doesnt work like this anymore, ie the StockDataUpdateSource doesnt dictate what source to use, it simply tells what source has been last used for given property update
                var stock = _ctx.Stocks.Include(s => s.StockDataUpdateSources).FirstOrDefault(s => s.Ticker == reply.price.symbol && s.StockDataUpdateSources.Any(d => d.Source == DataUpdateSource.RapidApiStockDataYFAlternative));
                if (stock == null)
                {
                    _log.Warn($"Received RapidApiStockDataYFAlternative data for stock {reply.price.symbol} but it either doesnt exist in db or doesnt have this datasouce - skipping");
                    continue;
                }

                stock.UpdatePrice(reply.price.regularMarketPrice.raw, DataUpdateSource.RapidApiStockDataYFAlternative, _ctx);
                stock.UpdateDividend(reply.summaryDetail.dividendRate.raw, DataUpdateSource.RapidApiStockDataYFAlternative, _ctx);
                stock.UpdateEps(reply.defaultKeyStatistics.trailingEps.raw, DataUpdateSource.RapidApiStockDataYFAlternative, _ctx);     
            }

            if (usedRequests > 0)
                _ctx.ApiUpdateRuns.Add(new ApiUpdateRun() { TimestampUtc = DateTime.UtcNow, UpdateType = updateType });

            await _ctx.SaveChangesAsync();
            return usedRequests;
        }


        public async Task UploadUsCcc(string fileWithPath)
        {
            _log.Debug("Starting to process US CCC excel update...");

            var ctx = _ctx;
            const int EKARIVI = 3; //Eka varsinainen datarivi otsikkojen yms sälän jälkeen. Indeksointi alkaa nollata, tämä siis excelin 4. rivi
            int updates = 0;
            int additions = 0;
            int removed = 0;

            System.IO.StreamReader file = new System.IO.StreamReader(fileWithPath);
            var inputStream = new FileStream(fileWithPath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook = WorkbookFactory.Create(inputStream);
            workbook.MissingCellPolicy = MissingCellPolicy.RETURN_NULL_AND_BLANK;

            ISheet cccSheet = workbook.GetSheet("All");
            var fileUpdated = cccSheet.GetRow(1).GetCell(0).DateCellValue;

            List<StockDTO> NewStockData = new List<StockDTO>();

            for (int row = EKARIVI; row <= cccSheet.LastRowNum; row++)
            {
                StockDTO s = new StockDTO();
                s.Currency = Currency.USD;
                //s.DataLastUpdated = fileUpdated;
                var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

                try
                {
                    var firstCellInRow = cccSheet.GetRow(row).GetCell(0);
                    if (firstCellInRow == null || firstCellInRow.StringCellValue == null || firstCellInRow.StringCellValue == "")
                        break;
                    //var name = firstCellInRow.StringCellValue;
                    s.Name = cccSheet.GetRow(row).GetCell(1).StringCellValue;
                    s.Ticker = cccSheet.GetRow(row).GetCell(0).StringCellValue;                   
                    s.Sector = cccSheet.GetRow(row).GetCell(3).StringCellValue;
                    s.DGYears = (int)Math.Ceiling(cccSheet.GetRow(row).GetCell(4).NumericCellValue);
                    s.CurrentPrice = cccSheet.GetRow(row).GetCell(5).NumericCellValue;
                    s.PriceLastUpdated = fileUpdated;
                    s.Dividend = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(10), evaluator);
                    var pe = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(35));
                    s.EpsTtm = pe > 0.000001 ? s.CurrentPrice / pe : 0.0;
                    //s.EpsGrowth5 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(35));
                    //s.DeptToEquity = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(39), evaluator);
                    s.DivGrowth1 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(16), evaluator);
                    s.DivGrowth3 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(17), evaluator);
                    s.DivGrowth5 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(18), evaluator);
                    s.DivGrowth10 = NpoiHelper.GetDoubleValue(cccSheet.GetRow(row).GetCell(19), evaluator);
                    //s.PriceSource = PriceSource.FMP;
                    //s.DataUpdateSource = DataUpdateSource.US_CCC;
                    //s.Exchange = Exchange.NyseNasdaq;
                }
                catch (Exception e)
                {
                    _log.Error("Invalid datafield while reading stock row: " + row + ". Problem: " + e.Message);
                    break;
                }
                
                NewStockData.Add(s);               
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
                    //if (fileUpdated > dbstock.DataLastUpdated) //TODO: Should check values individually like when fetching prices etc from apis
                    //{
                        dbstock.UpdateDgYears(s.DGYears, DataUpdateSource.US_CCC, ctx, fileUpdated);
                        dbstock.UpdateDividend(s.Dividend, DataUpdateSource.US_CCC, ctx, fileUpdated);
                        dbstock.UpdateEps(s.EpsTtm, DataUpdateSource.US_CCC, ctx, fileUpdated);
                        _log.Debug($"Trying to set {s.Ticker} EpsTtm = {s.EpsTtm}");
                        dbstock.UpdateEpsGrowth5(s.EpsGrowth5, DataUpdateSource.US_CCC, ctx, fileUpdated);
                        dbstock.UpdateDividendGrowth(s.DivGrowth1, s.DivGrowth3, s.DivGrowth5, s.DivGrowth10, DataUpdateSource.US_CCC);
                        //dbstock.DeptToEquity = s.DeptToEquity; //Not available in Dividend Radar excel
                        //dbstock.DataLastUpdated = fileUpdated;
                        //if (dbstock.PriceLastUpdated < fileUpdated)
                        //{
                            dbstock.UpdatePrice(s.CurrentPrice, DataUpdateSource.US_CCC, ctx, fileUpdated);
                        //}
                        dbstock.SetCccStatus(true);
                        updates++;
                    //}
                    }
                    else 
                    {
                        sbAdd.AppendLine(s.Ticker + " (" + s.Name + ")");
                        Stock stock = new Stock(s.Ticker, s.Name, s.CurrentPrice, s.Dividend, s.DGYears, s.EpsTtm, s.Currency, Exchange.NyseNasdaq, DataUpdateSource.US_CCC, s.Sector, fileUpdated);
                        stock.UpdateEpsGrowth5(s.EpsGrowth5, DataUpdateSource.US_CCC, ctx, fileUpdated);
                        ctx.Stocks.Add(stock);
                        additions++;
                    }
                }
                _log.Debug(sbAdd.ToString());

                //Update stocks that have datasource as US_CCC but are not in new excel
                var missingInFile = ctx.Stocks.Where(ss => ss.CccStock).ToList().Where(st => !NewStockData.Any(n => n.Ticker == st.Ticker)).Select(t => t.Ticker).ToList();

                StringBuilder sbDel = new StringBuilder();
                sbDel.AppendLine("Deleted the following stocks from database entirely due to them being no longer in US CCC list:");
                StringBuilder sbDown = new StringBuilder();
                sbDown.AppendLine("Downgraded the following stocks to manual update due to them no longer being in US CCC list:");
                foreach (var ticker in missingInFile)
                {
                    var missing = ctx.Stocks.FirstOrDefault(sto => sto.Ticker == ticker);

                    var existsInWatchlist = ctx.Watchlists.SelectMany(w => w.WatchlistStocks).FirstOrDefault(w => w.Stock.Ticker == ticker) != null;
                    var existsInPortfolio = ctx.Portfolios.Any(po => po.Positions.Any(p => p.Stock.Ticker == ticker));

                    //If stock is not in any "use", remove entirely
                    if (!existsInWatchlist && !existsInPortfolio)
                    {
                        sbDel.AppendLine(missing.Ticker + " (" + missing.Name + ")");
                        int? ssdScoreId = missing.SSDSafetyScore != null ? missing.SSDSafetyScore.Id : null;
                        ctx.Stocks.Remove(missing);
                        if (ssdScoreId != null)
                            ctx.SSDScore.Remove(ctx.SSDScore.FirstOrDefault(ssd => ssd.Id == ssdScoreId));

                    }
                    else
                    {
                        sbDown.AppendLine(missing.Ticker + " (" + missing.Name + ")");
                        missing.SetCccStatus(false);
                        missing.UpdateDgYears(0, DataUpdateSource.US_CCC, ctx, fileUpdated); //Ettei ainakaan jää vanha data!
                    }
                    removed++;
                }
                _log.Debug(sbDown.ToString());
                _log.Debug(sbDel.ToString());
            }
            catch (Exception e)
            {
                _log.Error("Failure updating stocks in database. Problem: " + e.Message);
                file.Close();
                file.Dispose();
                return;
            }

            file.Close();
            file.Dispose();

            await ctx.SaveChangesAsync();

            _log.Debug("US CCC update done. Updated " + updates + ", added " + additions + " and stripped from ccc " + removed + " stocks in database");
        }


        public IEnumerable<HistoricalDividendDto> GetDividendHistory(int stockId)
        {
            return _ctx.HistoricalDividend.Where(h => h.StockId == stockId).Select(s => s.ToDTO()).ToList();
        }

        public IEnumerable<HistoricalPriceDto> GetPriceHistory(int stockId)
        {
            return _ctx.HistoricalPrice.Where(h => h.StockId == stockId).Select(s => s.ToDTO()).ToList();
        }

        public async void Dispose()
        {
            _log.Debug("Disposing StockService #" + InstanceNumber);
            await _ctx.DisposeAsync();
            _client.Dispose();
        }
    }
}
