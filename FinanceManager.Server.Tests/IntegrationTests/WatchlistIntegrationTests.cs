using Akka.Configuration.Hocon;
using Common;
using Common.Dtos;
using Common.HelperModels;
using Financemanager.Server.Database.Domain;
using FinanceManager.Server.Database;
using FinanceManager.Server.IntegrationTests;
using FinanceManager.Server.IntegrationTests.Util;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SendGrid.Helpers.Mail;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FinanceManager.Server.Tests.IntegrationTests
{
    public class WatchlistIntegrationTests : IClassFixture<FmWebApplicationFactory<Program>>, IClassFixture<FmFixture>
    {
        private HttpClient _httpClient;
        private readonly FmWebApplicationFactory<Program> _factory;
        private readonly FmFixture _fixture;
        private static bool seeded = false;

        public WatchlistIntegrationTests(FmWebApplicationFactory<Program> factory, FmFixture fixture)
        {
            _factory = factory;
            _fixture = fixture;
            _httpClient = factory.CreateClient();

            using (var scope = _factory.Services.CreateScope())
            {
                _fixture.EnsureSeeded(scope);
            }
        }


        [Fact]
        public async void Testuser_login_should_work()
        {
            var content = new { username = "test", password = "1test2" };
            var json = JsonSerializer.Serialize(content);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            await Task.Delay(2500);

            System.Diagnostics.Debug.WriteLine("Making login request test");
            var response = await _httpClient.PostAsync(@"http://localhost:6001/login", stringContent);
            var jsonString = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JwtToken>(jsonString);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(token.accessToken);
            Assert.NotNull(token.refreshToken);
            Assert.NotNull(token.expires);
            //TODO: Content validation, should contain the jwt token structure (accessToken, refreshToken, expires)
        }

        [Fact]
        public async void Getting_watchlist_should_work()
        {
            var accessToken = await GetTestUserAccessToken();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(@"http://localhost:6001/api/watchlist/"),
                Method = HttpMethod.Get,
            };
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await _httpClient.SendAsync(request);
            var watchlist = ExtendedJsonSerializer.Deserialize<WatchlistDTO>(await resp.Content.ReadAsStringAsync());

            Assert.True(resp.IsSuccessStatusCode);
            Assert.NotNull(watchlist);
        }

        [Fact]
        public async void Adding_to_watchlist_should_work()
        {
            //TODO: Should rewrite this, and use simple stock + targetprice etc additional info, instead of this class for adding etc. Or at least remove stock related stuff from watchlistStock
            //Make sure test starts with empty watchlist, created by seeder before these tests
            Stock cvxStock;
            using (var ctx = _fixture.CreateFmTestContext())
            {
                ctx.Database.ExecuteSql($"DELETE FROM WatchlistStock WHERE WatchlistId = 1");
                Stock? stock = ctx.Stocks.FirstOrDefault(s => s.Ticker == "CVX");
                if (stock == null)
                    throw new NullReferenceException("Cannot find seeded stock in database");
                cvxStock = stock;
            }

            var now = DateTime.UtcNow;
            
            var wls = new WatchlistStockDTO() { StockTicker = "CVX",  StockId = cvxStock.Id };
            var accessToken = await GetTestUserAccessToken();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(@"http://localhost:6001/api/watchlist/1/stocks"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(wls), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);

            var getReq = new HttpRequestMessage
            {
                RequestUri = new Uri(@"http://localhost:6001/api/watchlist"),
                Method = HttpMethod.Get,
            };
            getReq.Headers.Add("Authorization", "Bearer " + accessToken);
            getReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var portResp = await _httpClient.SendAsync(getReq);

            Assert.NotNull(portResp);
            Assert.True(portResp.IsSuccessStatusCode);

            var jsonString = await portResp.Content.ReadAsStringAsync();
            var watchlist = ExtendedJsonSerializer.Deserialize<WatchlistDTO>(jsonString);

            Assert.NotNull(watchlist);
            Assert.True(watchlist.Stocks.Count == 1);
            Assert.True(watchlist.Stocks[0].StockTicker == "CVX");
        }


        [Fact]
        public async Task Deleting_from_watchlist_should_work()
        {
            //Add new watchlist
            string testUserId = "";
            using (var ctx = _fixture.CreateAuthTestContext())
            {
                testUserId = ctx.Users.First(u => u.UserName == "test").Id.ToString(); //Tämä tulee tässä haettaessa oikein, mutta menee kantaan muodossa @p0 alla skriptissä, jotakin vikaa.
            }

            int wlId = 0;
            int wlsId = 0;
            using (var ctx = _fixture.CreateFmTestContext())
            {
                var wl = ctx.Watchlists.Add(new Watchlist(testUserId));
                await ctx.SaveChangesAsync();
                wlId = wl.Entity.Id;
                var stock = ctx.Stocks.First();
                wl.Entity.AddOrUpdateStock(stock, 10.0, false);
                await ctx.SaveChangesAsync();
                wlsId = wl.Entity.WatchlistStocks.First().Id;
            }

            var accessToken = await GetTestUserAccessToken();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(@$"http://localhost:6001/api/watchlist/{wlId}/stocks/{wlsId}"),
                Method = HttpMethod.Delete,
            };
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await _httpClient.SendAsync(request);
            var watchlist = ExtendedJsonSerializer.Deserialize<WatchlistDTO>(await resp.Content.ReadAsStringAsync());

            Assert.True(resp.IsSuccessStatusCode);
            Assert.NotNull(watchlist);
            Assert.True(watchlist.Id == wlId);
            Assert.True(watchlist.Stocks.Count == 0);
        }



        private async Task<string> GetTestUserAccessToken()
        {
            var content = new { username = "test", password = "1test2" };
            var json = JsonSerializer.Serialize(content);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(@"http://localhost:6001/login", stringContent);
            var jsonString = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JwtToken>(jsonString);
            return token.accessToken;
        }


    }

}