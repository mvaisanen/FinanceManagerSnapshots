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
    public class RapidApiYHFinanceClient : IDisposable
    {
        private Logger _log;
        public readonly int InstanceNumber;
        private static int InstancesCreated;
        private HttpClient _client;
        public static readonly string ClientName = "RapidApiYHFinance";

        public RapidApiYHFinanceClient(HttpClient httpClient)
        {
            _log = LogManager.GetCurrentClassLogger();
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);
            _log.Debug($"RapidApiYHFinanceClient #{InstanceNumber} created");
            _client = httpClient;
        }

        public async Task<RapidApiYHFinanceQuoteReply> GetQuotes(string[] tickers)
        {
            _log.Debug("Trying to request RapidApiYHFinance QuoteReply...");
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
                RequestUri = new Uri($"https://yh-finance.p.rapidapi.com/market/v2/get-quotes?region=US&symbols={tickersStr}"),
                Headers =
                {
                    { "x-rapidapi-host", "yh-finance.p.rapidapi.com" },
                    { "x-rapidapi-key", apiKey },
                },
            };

            try
            {
                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _log.Debug($"RapidApiYHFinance reply for quote query for {tickersStr}: \n" + jsonString);

                    var reply = JsonSerializer.Deserialize<RapidApiYHFinanceQuoteReply>(jsonString);
                    if (string.IsNullOrEmpty(reply.quoteResponse.error))
                    {
                        return reply;
                    }
                    else
                    {
                        _log.Error("RapidApiYHFinance replied with error: " + reply.quoteResponse.error);
                        throw new HttpRequestException($"API replied with error: {reply.quoteResponse.error}");
                    }
                }
                else
                {
                    _log.Error($"Failed to query RapidApiYHFinance quote for tickers {tickersStr}: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new HttpRequestException("Request response indicates failure", null, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Exception when executing RapidApiYHFinance quote query: {e} - {e.Message}");
                throw new HttpRequestException($"Request exception: {e} - {e.Message}", e);
            }
        }


        public void Dispose()
        {
      
        }
    }
}
