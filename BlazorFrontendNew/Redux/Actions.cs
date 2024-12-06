using BlazorFrontendNew.BlazorRedux;
using BlazorFrontendNew.Client.Util;
using Common;
using Microsoft.JSInterop;
using Common.Dtos;
using Common.HelperModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorFrontendNew.Redux
{
    public static class Actions
    {

        private const string baseUrl = "http://localhost:6001";

        public async static void Login(HttpClient client, string userName, string pasword, Action<IAction> Dispatch)
        {
            Dispatch(new LoginStartedAction());
            var content = new { username = userName, password = pasword };
            var json = JsonSerializer.Serialize(content);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{baseUrl}/login", stringContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Dispatch(new LoginSucceededAction(jsonString));
                    Dispatch(new NewLocationAction() { Location = "/" });
                }
                else
                {
                    Dispatch(new LoginFailedAction(response.ReasonPhrase));
                }
            }
            catch(Exception e)
            {
                Dispatch(new LoginFailedAction(e.Message));
            }
        }

        public async static void Logout(HttpClient client, string jwt, Action<IAction> Dispatch)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                Dispatch(new LogoutAction()); //Nothing to do
                return;
            }

            var emptyStringContent = new StringContent("", Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/logout", emptyStringContent);

            Dispatch(new LogoutAction());
        }

        public async static void GetWatchlist(HttpClient client, string jwt, Action<IAction> Dispatch)
        {
            Console.WriteLine("GetWatchlist(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new WatchlistFetchFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api" + "/watchlist"),
                Method = HttpMethod.Get,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Fetched watchlist as string: " + jsonString);
                var watchlist = ExtendedJsonSerializer.Deserialize<WatchlistDTO>(jsonString);
                Dispatch(new WatchlistFetchSucceededAction(watchlist));
            }
            else
            {
                Dispatch(new WatchlistFetchFailedAction("Unable to load watchlist from server: " + response.ReasonPhrase));
            }

        }


        public async static void RemoveFromWatchlist(HttpClient client, string jwt, Action<IAction> Dispatch, int watchlistId, int wlStockId)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                //Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new RemoveFromWatchlistFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/watchlist/{watchlistId}/stocks/{wlStockId}"),
                Method = HttpMethod.Delete,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            //Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine("Fetched watchlist as string: " + jsonString);
                var watchlist = ExtendedJsonSerializer.Deserialize<WatchlistDTO>(jsonString);
                Dispatch(new RemoveFromWatchlistSucceededAction(watchlist));
            }
            else
            {
                Dispatch(new RemoveFromWatchlistFailedAction("Failed to remove stock from watchlist: " + response.ReasonPhrase));
            }

        }


        public async static void SaveWatchlistStock(HttpClient client, string jwt, Action<IAction> Dispatch, int watchlistId, WatchlistStockDTO wls)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                //Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new SaveWatchlistStockFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/watchlist/{watchlistId}/stocks/"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(wls), Encoding.UTF8, "application/json")
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            //Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine("Fetched watchlist as string: " + jsonString);
                var watchlist = ExtendedJsonSerializer.Deserialize<WatchlistDTO>(jsonString);
                Dispatch(new SaveWatchlistStockSucceededAction(watchlist));
            }
            else
            {
                Dispatch(new SaveWatchlistStockFailedAction("Failed to save watchlist stock: " + response.ReasonPhrase));
            }

        }


        //TODO: Ei tehdäkään tästä redux versiota vaan suoraan kutsuttava (async) funktio
        public async static Task<List<StockDTO>> FindClosestStockMatches(HttpClient client, string jwt, string searchParam)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("Jwt is null or empty, throwing error...");
                throw new ArgumentException("Token is missing, cannot make the request"); //should never happen 
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/stock/matches/{searchParam}"),
                Method = HttpMethod.Get,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("FindClosestStockMatches: " + jsonString);
                var stocks = ExtendedJsonSerializer.Deserialize<List<StockDTO>>(jsonString);
                return stocks;
            }
            else
            {
                throw new InvalidOperationException("Finding closest matches failed: " + response.ReasonPhrase);
            }

        }

        public async static void GetPortfolio(HttpClient client, string jwt, Action<IAction> Dispatch)
        {
            Console.WriteLine("GetPortfolio(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new PortfolioFetchFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api" + "/portfolio"),
                Method = HttpMethod.Get,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                Console.WriteLine("await client.SendAsync(request)");
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Fetched portfolio as string: " + jsonString);
                    try
                    {
                        Console.WriteLine("Deserializing json...");
                        var portfolio = ExtendedJsonSerializer.Deserialize<PortfolioDto>(jsonString);
                        Console.WriteLine("Deserialized object PortfolioDto, id="+portfolio.Id);
                        Dispatch(new PortfolioFetchSucceededAction(portfolio));
                    }
                    catch(JsonException jse)
                    {
                        Dispatch(new PortfolioFetchFailedAction("Unable to deserialize server response: " + jse.Message));
                    }
                }
                else
                {
                    Dispatch(new PortfolioFetchFailedAction("Unable to load portfolio from server: " + response.ReasonPhrase));
                }
            }
            catch(Exception e)
            {
                Dispatch(new PortfolioFetchFailedAction("Unable to load portfolio from server: " + e.Message));
            }

        }


        public async static void AddPurchase(HttpClient client, string jwt, Action<IAction> Dispatch, AddToPortfolioDto purchase)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                //Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new AddPurchaseFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/portfolio"), //TODO: Serverille ehkä mieluummin api/portfolio/{id}/purchase POST
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(purchase), Encoding.UTF8, "application/json")
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            //Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine("Fetched watchlist as string: " + jsonString);
                var portfolio = ExtendedJsonSerializer.Deserialize<PortfolioDto>(jsonString);
                Dispatch(new AddPurchaseSucceededAction(portfolio));
            }
            else
            {
                Dispatch(new AddPurchaseFailedAction("Failed to add purchase: " + response.ReasonPhrase));
            }

        }

        public async static void EditPurchase(HttpClient client, string jwt, Action<IAction> Dispatch, StockPurchaseDto purchase)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                Dispatch(new EditPurchaseFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/portfolio/purchases/{purchase.Id}"), //TODO: Serverille ehkä mieluummin api/portfolio/{id}/purchase POST
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(purchase), Encoding.UTF8, "application/json")
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var portfolio = ExtendedJsonSerializer.Deserialize<PortfolioDto>(jsonString);
                    Dispatch(new EditPurchaseSucceededAction(portfolio));
                }
                else
                {
                    Dispatch(new EditPurchaseFailedAction("Failed to edit purchase: " + response.ReasonPhrase));
                }
            }
            catch(Exception e)
            {
                Dispatch(new EditPurchaseFailedAction("Failed to edit purchase: An exception occured when trying to communicate with server" + e.Message));
            }

        }


        public async static void GetFxRates(HttpClient client, string jwt, Action<IAction> Dispatch)
        {
            Console.WriteLine("GetFxRates(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new FxRatesFetchFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api" + "/fxrates/current"),
                Method = HttpMethod.Get,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                Console.WriteLine("await client.SendAsync(request)");
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Fetched fxrates as string: " + jsonString);
                    try
                    {
                        Console.WriteLine("Deserializing json...");
                        var fxrates = ExtendedJsonSerializer.Deserialize<CurrencyRatesDto>(jsonString);
                        Dispatch(new FxRatesFetchSucceededAction(fxrates));
                    }
                    catch (JsonException jse)
                    {
                        Dispatch(new FxRatesFetchFailedAction("Unable to deserialize server response: " + jse.Message));
                    }
                }
                else
                {
                    Dispatch(new FxRatesFetchFailedAction("Unable to load fxrates from server: " + response.ReasonPhrase));
                }
            }
            catch (Exception e)
            {
                Dispatch(new FxRatesFetchFailedAction("Unable to load fxrates from server: " + e.Message));
            }

        }


        public async static void GetDividends(HttpClient client, string jwt, Action<IAction> Dispatch)
        {
            Console.WriteLine("GetDividends(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new DividendsFetchFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api" + "/dividends"),
                Method = HttpMethod.Get,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                Console.WriteLine("await client.SendAsync(request)");
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Fetched dividends as string: " + jsonString);
                    try
                    {
                        Console.WriteLine("Deserializing json...");
                        var divs = ExtendedJsonSerializer.Deserialize<List<ReceivedDividendDTO>>(jsonString);
                        Dispatch(new DividendsFetchSucceededAction(divs));
                    }
                    catch (JsonException jse)
                    {
                        Dispatch(new DividendsFetchFailedAction("Unable to deserialize server response: " + jse.Message));
                    }
                }
                else
                {
                    Dispatch(new DividendsFetchFailedAction("Unable to load dividends from server: " + response.ReasonPhrase));
                }
            }
            catch (Exception e)
            {
                Dispatch(new DividendsFetchFailedAction("Unable to load dividends from server: " + e.Message));
            }

        }


        public async static void UploadIbPortfolioCsv(HttpClient client, string jwt, Action<IAction> Dispatch, FileUpload csvFile)
        {
            Console.WriteLine("UploadIbPortfolioCsv(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Dispatch(new AddPurchaseFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/portfolio/uploadibscv"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(csvFile), Encoding.UTF8, "application/json")
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var portfolio = ExtendedJsonSerializer.Deserialize<PortfolioDto>(jsonString);
                Dispatch(new IbPortfolioUploadSucceededAction(portfolio));
            }
            else
            {
                Dispatch(new IbPortfolioUploadFailedAction("Failed to upload IB csv: " + response.ReasonPhrase));
            }

        }


        public async static void DeletePortfolioPosition(HttpClient client, string jwt, Action<IAction> Dispatch, int portfolioId, int positionId)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/portfolio/{portfolioId}/positions/{positionId}"),
                Method = HttpMethod.Delete,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine("Fetched watchlist as string: " + jsonString);
                var portfolio = ExtendedJsonSerializer.Deserialize<PortfolioDto>(jsonString);
                Dispatch(new DeletePortfolioPositionSucceededAction(portfolio));
            }
            else
            {
                Dispatch(new DeletePortfolioPositionFailedAction("Failed to remove position from portfolio: " + response.ReasonPhrase));
            }

        }


        public async static void UploadIbDividendsCsv(HttpClient client, string jwt, Action<IAction> Dispatch, FileUpload csvFile, bool multiAccount=false)
        {
            Console.WriteLine("UploadIbDividendsCsv(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Dispatch(new IbDividendsUploadFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/dividends/uploadibcsv?multiaccount={multiAccount}"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(csvFile), Encoding.UTF8, "application/json")
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var changes = ExtendedJsonSerializer.Deserialize<DividendsChangedDto>(jsonString);
                    Dispatch(new IbDividendsUploadSucceededAction(changes));
                }
                else
                {
                    Dispatch(new IbDividendsUploadFailedAction("Failed to upload IB dividends csv: " + response.ReasonPhrase));
                }
            }
            catch(Exception e)
            {
                Dispatch(new IbDividendsUploadFailedAction("Failed to upload IB dividends csv: " + e.Message));
            }

        }


        public async static void UploadNordnetDividendsCsv(HttpClient client, string jwt, Action<IAction> Dispatch, FileUpload csvFile)
        {
            Console.WriteLine("UploadNordnetDividendsCsv(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Dispatch(new NordnetDividendsUploadFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/dividends/uploadnordnetcsv"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(csvFile), Encoding.UTF8, "application/json")
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var changes = ExtendedJsonSerializer.Deserialize<DividendsChangedDto>(jsonString);
                    Dispatch(new NordnetDividendsUploadSucceededAction(changes));
                }
                else
                {
                    Dispatch(new NordnetDividendsUploadFailedAction("Failed to upload Nordnet dividends csv: " + response.ReasonPhrase));
                }
            }
            catch (Exception e)
            {
                Dispatch(new NordnetDividendsUploadFailedAction("Failed to upload Nordnet dividends csv: " + e.Message));
            }

        }


        public async static void UploadDividendPlan(HttpClient client, string jwt, Action<IAction> Dispatch, DividendsPlanDto plan)
        {
            Console.WriteLine("UploadDividendPlan(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Dispatch(new DividendPlanUploadFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api/dividends/plans"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(plan), Encoding.UTF8, "application/json")
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var planUploaded = ExtendedJsonSerializer.Deserialize<DividendsPlanDto>(jsonString);
                    Dispatch(new DividendPlanUploadSucceededAction(planUploaded));
                }
                else
                {
                    Dispatch(new DividendPlanUploadFailedAction("Failed to upload dividend plan: " + response.ReasonPhrase));
                }
            }
            catch (Exception e)
            {
                Dispatch(new DividendPlanUploadFailedAction("Failed to upload dividend plan: " + e.Message));
            }

        }


        public async static void GetDividendPlan(HttpClient client, string jwt, Action<IAction> Dispatch)
        {
            Dispatch(new DividendPlanFetchStartedAction());
            Console.WriteLine("GetDividendPlan(): Token is " + jwt);
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("Jwt is null or empty, dispatching fail...");
                Dispatch(new DividendPlanFetchFailedAction("Token is missing, cannot make the request")); //should never happen 
                return;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{baseUrl}/api" + "/dividends/plans"),
                Method = HttpMethod.Get,
            };

            var token = ExtendedJsonSerializer.Deserialize<JwtToken>(jwt);
            Console.WriteLine("Serialized jwt, accessToken=" + token.accessToken);
            request.Headers.Add("Authorization", "Bearer " + token.accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                Console.WriteLine("Requesting dividend plan from server...");
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Fetched dividend plan as string: " + jsonString);
                    try
                    {
                        Console.WriteLine("Deserializing json...");
                        var plan = ExtendedJsonSerializer.Deserialize<DividendsPlanDto>(jsonString);
                        Dispatch(new DividendPlanFetchSucceededAction(plan));
                    }
                    catch (JsonException jse)
                    {
                        Dispatch(new DividendPlanFetchFailedAction("Unable to deserialize server response: " + jse.Message));
                    }
                }
                else
                {
                    Dispatch(new DividendPlanFetchFailedAction("Unable to load dividends plan from server: " + response.ReasonPhrase));
                }
            }
            catch (Exception e)
            {
                Dispatch(new DividendPlanFetchFailedAction("Unable to load dividends plan from server: " + e.Message));
            }

        }

    }
}
