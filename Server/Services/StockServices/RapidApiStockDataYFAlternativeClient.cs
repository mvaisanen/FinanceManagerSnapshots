using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Services.StockServices
{
    public class RapidApiStockDataYFAlternativeClient: IDisposable
    {
        private Logger _log;
        public readonly int InstanceNumber;
        private static int InstancesCreated;
        private HttpClient _client;
        public static readonly string ClientName = "RapidApiStockDataYFAlternative";

        public RapidApiStockDataYFAlternativeClient(HttpClient httpClient)
        {
            _log = LogManager.GetCurrentClassLogger();
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);
            _log.Debug($"RapidApiStockDataYFAlternativeClient #{InstanceNumber} created");
            _client = httpClient;
        }

        public async Task<RapidApiStockDataYFAlternativeQuoteReply> GetQuotes(string[] tickers)
        {
            _log.Debug("Trying to request RapidApiStockDataYFAlternative QuoteReply...");
            if (tickers.Count() > 10)
                throw new ArgumentException("Max 10 tickers allowed per query");
            var tickersStr = String.Join("%2C", tickers);

            var apiKey = System.Environment.GetEnvironmentVariable("RAPIDAPI_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve RapidApi apiKey from Environment settings. Exiting update.");
                throw new NullReferenceException("No RapidApi key found");
            }
            _log.Debug("Found RapidApi API key: " + apiKey);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                //RequestUri = new Uri($"https://stock-data-yahoo-finance-alternative.p.rapidapi.com/v6/finance/quote?symbols=CVX%2CTOKMAN.HE%2CMUV2.DE"),
                RequestUri = new Uri($"https://stock-data-yahoo-finance-alternative.p.rapidapi.com/v6/finance/quote?symbols={tickersStr}"),
                Headers =
                {
                    { "x-rapidapi-host", "stock-data-yahoo-finance-alternative.p.rapidapi.com" },
                    { "x-rapidapi-key", apiKey },
                },
            };

            try
            {
                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _log.Debug($"RapidApiStockDataYFAlternative reply for quote query for {tickersStr}: \n" + jsonString);

                    var reply = JsonSerializer.Deserialize<RapidApiStockDataYFAlternativeQuoteReply>(jsonString);
                    if (string.IsNullOrEmpty(reply.quoteResponse.error))
                    {
                        /*foreach (var item in reply.quoteResponse.result)
                        {
                            _log.Debug($"Symbol: {item.symbol}, price: {item.regularMarketPrice}, eps: {item.epsTrailingTwelveMonths}, div: {item.trailingAnnualDividendRate}");
                        }*/
                        return reply;
                    }
                    else
                    {
                        _log.Error("RapidApiStockDataYFAlternative replied with error: " + reply.quoteResponse.error);
                        throw new HttpRequestException($"API replied with error: {reply.quoteResponse.error}");
                    }
                }
                else
                {
                    _log.Error($"Failed to query RapidApiStockDataYFAlternative quote for tickers {tickersStr}: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new HttpRequestException("Request response indicates failure", null, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Exception when executing RapidApiStockDataYFAlternative quote query: {e} - {e.Message}");
                throw new HttpRequestException($"Request exception: {e} - {e.Message}", e);
            }
        }

        public async Task<RapidApiStockDataYFAlternativeDetailsReply> GetDetails(string ticker)
        {
            _log.Debug($"Trying to request RapidApiStockDataYFAlternative DetailsReply for {ticker}...");

            var apiKey = System.Environment.GetEnvironmentVariable("RAPIDAPI_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve RapidApi apiKey from Environment settings. Exiting update.");
                throw new NullReferenceException("No RapidApi key found");
            }
            _log.Debug("Found RapidApi API key: " + apiKey);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://stock-data-yahoo-finance-alternative.p.rapidapi.com/v11/finance/quoteSummary/{ticker}?modules=summaryDetail%2Cprice%2CdefaultKeyStatistics"),
                Headers =
                {
                    { "x-rapidapi-host", "stock-data-yahoo-finance-alternative.p.rapidapi.com" },
                    { "x-rapidapi-key", apiKey },
                },
            };

            try
            {
                /*
                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _log.Debug($"RapidApiStockDataYFAlternative reply for quote query for {ticker}: \n" + jsonString);

                    var reply = JsonSerializer.Deserialize<RapidApiStockDataYFAlternativeDetailsReply>(jsonString);
                    if (string.IsNullOrEmpty(reply.quoteSummary.error))
                    {
                        return reply;
                    }
                    else
                    {
                        _log.Error("RapidApiStockDataYFAlternative replied with error: " + reply.quoteSummary.error);
                        throw new HttpRequestException($"API replied with error: {reply.quoteSummary.error}");
                    }
                }
                else
                {
                    _log.Error($"Failed to query RapidApiStockDataYFAlternative details for tickers {ticker}: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new HttpRequestException("Request response indicates failure", null, response.StatusCode);
                }*/
                var mockedReplyJson = "{\"quoteSummary\":{\"result\":[{\"summaryDetail\":{\"maxAge\":1,\"priceHint\":{\"raw\":2,\"fmt\":\"2\",\"longFmt\":\"2\"},\"previousClose\":{\"raw\":89.54,\"fmt\":\"89.54\"},\"open\":{\"raw\":89.75,\"fmt\":\"89.75\"},\"dayLow\":{\"raw\":89.59,\"fmt\":\"89.59\"},\"dayHigh\":{\"raw\":90.39,\"fmt\":\"90.39\"},\"regularMarketPreviousClose\":{\"raw\":89.54,\"fmt\":\"89.54\"},\"regularMarketOpen\":{\"raw\":89.75,\"fmt\":\"89.75\"},\"regularMarketDayLow\":{\"raw\":89.59,\"fmt\":\"89.59\"},\"regularMarketDayHigh\":{\"raw\":90.39,\"fmt\":\"90.39\"},\"dividendRate\":{\"raw\":3.16,\"fmt\":\"3.16\"},\"dividendYield\":{\"raw\":0.0353,\"fmt\":\"3.53 % \"},\"exDividendDate\":{\"raw\":1633564800,\"fmt\":\"2021 - 10 - 07\"},\"payoutRatio\":{\"raw\":0.3722,\"fmt\":\"37.22 % \"},\"fiveYearAvgDividendYield\":{\"raw\":3.79,\"fmt\":\"3.79\"},\"beta\":{\"raw\":0.859395,\"fmt\":\"0.86\"},\"trailingPE\":{\"raw\":10.655429,\"fmt\":\"10.66\"},\"forwardPE\":{\"raw\":11.846658,\"fmt\":\"11.85\"},\"volume\":{\"raw\":1292809,\"fmt\":\"1.29M\",\"longFmt\":\"1,292,809\"},\"regularMarketVolume\":{\"raw\":1292809,\"fmt\":\"1.29M\",\"longFmt\":\"1,292,809\"},\"averageVolume\":{\"raw\":5063193,\"fmt\":\"5.06M\",\"longFmt\":\"5,063,193\"},\"averageVolume10days\":{\"raw\":3695850,\"fmt\":\"3.7M\",\"longFmt\":\"3,695,850\"},\"averageDailyVolume10Day\":{\"raw\":3695850,\"fmt\":\"3.7M\",\"longFmt\":\"3,695,850\"},\"bid\":{\"raw\":90.38,\"fmt\":\"90.38\"},\"ask\":{\"raw\":90.39,\"fmt\":\"90.39\"},\"bidSize\":{\"raw\":0,\"fmt\":null,\"longFmt\":\"0\"},\"askSize\":{\"raw\":0,\"fmt\":null,\"longFmt\":\"0\"},\"marketCap\":{\"raw\":164509794304,\"fmt\":\"164.51B\",\"longFmt\":\"164,509,794,304\"},\"yield\":{},\"ytdReturn\":{},\"totalAssets\":{},\"expireDate\":{},\"strikePrice\":{},\"openInterest\":{},\"fiftyTwoWeekLow\":{\"raw\":57.44,\"fmt\":\"57.44\"},\"fiftyTwoWeekHigh\":{\"raw\":90.39,\"fmt\":\"90.39\"},\"priceToSalesTrailing12Months\":{\"raw\":3.8454838,\"fmt\":\"3.85\"},\"fiftyDayAverage\":{\"raw\":84.77853,\"fmt\":\"84.78\"},\"twoHundredDayAverage\":{\"raw\":85.2165,\"fmt\":\"85.22\"},\"trailingAnnualDividendRate\":{\"raw\":3.16,\"fmt\":\"3.16\"},\"trailingAnnualDividendYield\":{\"raw\":0.03529149,\"fmt\":\"3.53 % \"},\"navPrice\":{},\"currency\":\"CAD\",\"fromCurrency\":null,\"toCurrency\":null,\"lastMarket\":null,\"volume24Hr\":{},\"volumeAllCurrencies\":{},\"circulatingSupply\":{},\"algorithm\":null,\"maxSupply\":{},\"startDate\":{},\"tradeable\":false},\"defaultKeyStatistics\":{\"maxAge\":1,\"priceHint\":{\"raw\":2,\"fmt\":\"2\",\"longFmt\":\"2\"},\"enterpriseValue\":{\"raw\":-40194199552,\"fmt\":\" - 40.19B\",\"longFmt\":\" - 40,194,199,552\"},\"forwardPE\":{\"raw\":11.846658,\"fmt\":\"11.85\"},\"profitMargins\":{\"raw\":0.36606,\"fmt\":\"36.61 % \"},\"floatShares\":{\"raw\":1779323000,\"fmt\":\"1.78B\",\"longFmt\":\"1,779,323,000\"},\"sharesOutstanding\":{\"raw\":1820000000,\"fmt\":\"1.82B\",\"longFmt\":\"1,820,000,000\"},\"sharesShort\":{\"raw\":52877533,\"fmt\":\"52.88M\",\"longFmt\":\"52,877,533\"},\"sharesShortPriorMonth\":{\"raw\":54885372,\"fmt\":\"54.89M\",\"longFmt\":\"54,885,372\"},\"sharesShortPreviousMonthDate\":{\"raw\":1631664000,\"fmt\":\"2021 - 09 - 15\"},\"dateShortInterest\":{\"raw\":1634256000,\"fmt\":\"2021 - 10 - 15\"},\"sharesPercentSharesOut\":{\"raw\":0.0291,\"fmt\":\"2.91 % \"},\"heldPercentInsiders\":{\"raw\":9.0000004E-4,\"fmt\":\"0.09 % \"},\"heldPercentInstitutions\":{\"raw\":0.58107,\"fmt\":\"58.11 % \"},\"shortRatio\":{\"raw\":6.28,\"fmt\":\"6.28\"},\"shortPercentOfFloat\":{},\"beta\":{\"raw\":0.859395,\"fmt\":\"0.86\"},\"impliedSharesOutstanding\":{},\"morningStarOverallRating\":{},\"morningStarRiskRating\":{},\"category\":null,\"bookValue\":{\"raw\":51.215,\"fmt\":\"51.22\"},\"priceToBook\":{\"raw\":1.7649126,\"fmt\":\"1.76\"},\"annualReportExpenseRatio\":{},\"ytdReturn\":{},\"beta3Year\":{},\"totalAssets\":{},\"yield\":{},\"fundFamily\":null,\"fundInceptionDate\":{},\"legalType\":null,\"threeYearAverageReturn\":{},\"fiveYearAverageReturn\":{},\"priceToSalesTrailing12Months\":{},\"lastFiscalYearEnd\":{\"raw\":1604102400,\"fmt\":\"2020 - 10 - 31\"},\"nextFiscalYearEnd\":{\"raw\":1667174400,\"fmt\":\"2022 - 10 - 31\"},\"mostRecentQuarter\":{\"raw\":1627689600,\"fmt\":\"2021 - 07 - 31\"},\"earningsQuarterlyGrowth\":{\"raw\":0.577,\"fmt\":\"57.70 % \"},\"revenueQuarterlyGrowth\":{},\"netIncomeToCommon\":{\"raw\":15409999872,\"fmt\":\"15.41B\",\"longFmt\":\"15,409,999,872\"},\"trailingEps\":{\"raw\":8.483,\"fmt\":\"8.48\"},\"forwardEps\":{\"raw\":7.63,\"fmt\":\"7.63\"},\"pegRatio\":{\"raw\":0.61,\"fmt\":\"0.61\"},\"lastSplitFactor\":\"2:1\",\"lastSplitDate\":{\"raw\":1391385600,\"fmt\":\"2014 - 02 - 03\"},\"enterpriseToRevenue\":{\"raw\":-0.94,\"fmt\":\" - 0.94\"},\"enterpriseToEbitda\":{},\"52WeekChange\":{\"raw\":0.52434456,\"fmt\":\"52.43 % \"},\"SandP52WeekChange\":{\"raw\":0.3467741,\"fmt\":\"34.68 % \"},\"lastDividendValue\":{\"raw\":0.79,\"fmt\":\"0.79\"},\"lastDividendDate\":{\"raw\":1633564800,\"fmt\":\"2021 - 10 - 07\"},\"lastCapGain\":{},\"annualHoldingsTurnover\":{}},\"price\":{\"maxAge\":1,\"preMarketChange\":{},\"preMarketPrice\":{},\"postMarketChange\":{},\"postMarketPrice\":{},\"regularMarketChangePercent\":{\"raw\":0.009492947,\"fmt\":\"0.95 % \"},\"regularMarketChange\":{\"raw\":0.8499985,\"fmt\":\"0.85\"},\"regularMarketTime\":1635271148,\"priceHint\":{\"raw\":2,\"fmt\":\"2\",\"longFmt\":\"2\"},\"regularMarketPrice\":{\"raw\":90.39,\"fmt\":\"90.39\"},\"regularMarketDayHigh\":{\"raw\":90.39,\"fmt\":\"90.39\"},\"regularMarketDayLow\":{\"raw\":89.59,\"fmt\":\"89.59\"},\"regularMarketVolume\":{\"raw\":1292809,\"fmt\":\"1.29M\",\"longFmt\":\"1,292,809.00\"},\"averageDailyVolume10Day\":{\"raw\":3695850,\"fmt\":\"3.7M\",\"longFmt\":\"3,695,850\"},\"averageDailyVolume3Month\":{\"raw\":5063193,\"fmt\":\"5.06M\",\"longFmt\":\"5,063,193\"},\"regularMarketPreviousClose\":{\"raw\":89.54,\"fmt\":\"89.54\"},\"regularMarketSource\":\"FREE_REALTIME\",\"regularMarketOpen\":{\"raw\":89.75,\"fmt\":\"89.75\"},\"strikePrice\":{},\"openInterest\":{},\"exchange\":\"TOR\",\"exchangeName\":\"Toronto\",\"exchangeDataDelayedBy\":15,\"marketState\":\"REGULAR\",\"quoteType\":\"EQUITY\",\"symbol\":\"TD.TO\",\"underlyingSymbol\":null,\"shortName\":\"TORONTO - DOMINION BANK\",\"longName\":\"The Toronto-Dominion Bank\",\"currency\":\"CAD\",\"quoteSourceName\":\"Free Realtime Quote\",\"currencySymbol\":\"$\",\"fromCurrency\":null,\"toCurrency\":null,\"lastMarket\":null,\"volume24Hr\":{},\"volumeAllCurrencies\":{},\"circulatingSupply\":{},\"marketCap\":{\"raw\":164509794304,\"fmt\":\"164.51B\",\"longFmt\":\"164,509,794,304.00\"}}}],\"error\":null}}";
                return JsonSerializer.Deserialize<RapidApiStockDataYFAlternativeDetailsReply>(mockedReplyJson);
            }
            catch (Exception e)
            {
                _log.Error($"Exception when executing RapidApiStockDataYFAlternative details query: {e} - {e.Message}");
                throw new HttpRequestException($"Request exception: {e} - {e.Message}", e);
            }
        }

        public void Dispose()
        {
      
        }
    }
}
