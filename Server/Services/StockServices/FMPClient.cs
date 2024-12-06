using NLog;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Services.StockServices
{
    public class FMPClient: IDisposable
    {
        private Logger _log;
        private HttpClient _client;
        public static readonly string ClientName = "FMP";

        public FMPClient(HttpClient httpClient)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = httpClient;
        }

        /// <summary>
        /// Gets multiquote result from FMP api
        /// </summary>
        /// <param name="tickers">Stock tickers to get quote for. Maximum number 50</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<List<FMPStockQuoteResponse>> GetMultipleQuotes(string[] tickers)
        {
            if (tickers.Length > 1000)
                throw new ArgumentException("Too many tickers, maximum is 1000");

            var apiKey = System.Environment.GetEnvironmentVariable("FMP_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve FMP apiKey from Environment settings. Exiting update.");
                throw new NullReferenceException("FMP API key not found");
            }

            //https://financialmodelingprep.com/api/v3/quote/AAPL,FB?apikey=abc123def
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://financialmodelingprep.com/api/v3/quote/{string.Join(',', tickers)}?apikey={apiKey}"),
                Method = HttpMethod.Get,
            };
            request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

            var resp = new List<FMPStockQuoteResponse>();
            try
            {
                _log.Debug($"Requesting FMP  quote reply for tickers: {string.Join(',', tickers)}");
                var response = await _client.SendAsync(request);
                var jsonString = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {                   
                    _log.Debug($"FMP reply for quote query: \n" + jsonString);
                    try
                    {
                        resp = JsonSerializer.Deserialize<List<FMPStockQuoteResponse>>(jsonString);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Unable to deserialize FMP stock quote reply");
                        throw new HttpRequestException($"Unable to deserialize FMP stock quote reply", ex);
                    }
                }
                else
                {
                    _log.Error($"Failed to query FMP stock quote: {response.ReasonPhrase} - {jsonString}");
                    throw new HttpRequestException("Request response indicates failure", null, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Exception when executing FMP query: {e} - {e.Message}");
                throw new HttpRequestException($"Request exception: {e} - {e.Message}", e);
            }

            return resp;

        }


        public async Task<FMPDividendHistoryResponse> FetchDividendHistory(string ticker)
        {
            var apiKey = System.Environment.GetEnvironmentVariable("FMP_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve FMP apiKey from Environment settings. Exiting update.");
                throw new NullReferenceException("FMP API key not found");
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://financialmodelingprep.com/api/v3/historical-price-full/stock_dividend/{ticker}?apikey={apiKey}"),
                Method = HttpMethod.Get,
            };
            request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

            FMPDividendHistoryResponse resp = new FMPDividendHistoryResponse() { historical = new List<HistoricalDiv>() };
            try
            {
                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _log.Debug($"FMP reply for dividend history query for {ticker}: \n" + jsonString);
                    try
                    {
                        resp = JsonSerializer.Deserialize<FMPDividendHistoryResponse>(jsonString);
                    }
                    catch(Exception ex)
                    {
                        _log.Error("Unable to deserialize FMP stock dividend history reply");
                        throw new HttpRequestException($"Unable to deserialize FMP stock dividend history reply", ex);
                    }
                }
                else
                {
                    _log.Error($"Failed to query FMP div history for {ticker}: {response.ReasonPhrase}");
                    throw new HttpRequestException("Request response indicates failure", null, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Exception when executing FMP query: {e} - {e.Message}");
                throw new HttpRequestException($"Request exception: {e} - {e.Message}", e);
            }

            return resp;
        }

        public async Task<FMPPriceHistoryResponse> FetchPriceHistory(string ticker)
        {
            var apiKey = System.Environment.GetEnvironmentVariable("FMP_APIKEY", EnvironmentVariableTarget.User);
            if (apiKey == null)
            {
                _log.Error("Failed to retrieve FMP apiKey from Environment settings. Exiting update.");
                throw new NullReferenceException("FMP API key not found");
            }

            var now = DateTime.UtcNow.ToShortDateString().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?from=2000-01-01&to={now}&apikey={apiKey}"),
                Method = HttpMethod.Get,
            };
            request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

            FMPPriceHistoryResponse resp = new FMPPriceHistoryResponse() { historical = new List<StockHistoricalPrice>() };
            try
            {
                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _log.Debug($"FMP reply for price history query for {ticker}: \n" + jsonString);
                    try
                    {
                        resp = JsonSerializer.Deserialize<FMPPriceHistoryResponse>(jsonString);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Unable to deserialize FMP stock price history reply");
                        throw new HttpRequestException($"Unable to deserialize FMP stock price history reply", ex);
                    }
                }
                else
                {
                    _log.Error($"Failed to query FMP price history for {ticker}: {response.ReasonPhrase}");
                    throw new HttpRequestException("Request response indicates failure", null, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Exception when executing FMP query: {e} - {e.Message}");
                throw new HttpRequestException($"Request exception: {e} - {e.Message}", e);
            }

            return resp;
        }

        public void Dispose()
        {
           
        }
    }
}
