using Akka.Configuration.Hocon;
using Common;
using Common.Dtos;
using Common.HelperModels;
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
    public class PortfolioIntegrationTests : IClassFixture<FmWebApplicationFactory<Program>>, IClassFixture<FmFixture>
    {
        private HttpClient _httpClient;
        private readonly FmWebApplicationFactory<Program> _factory;
        private readonly FmFixture _fixture;

        public PortfolioIntegrationTests(FmWebApplicationFactory<Program> factory, FmFixture fixture)
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
        public async void Test_user_login_should_work()
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
        public async void Adding_to_portfolio_should_work()
        {
            //Make sure test starts with empty portfolio, created by seeder before these tests
            using (var ctx = _fixture.CreateFmTestContext())
            {
                ctx.Database.ExecuteSql($"DELETE FROM StockPurchase WHERE PortfolioPositionId IN (SELECT PortfolioPositionId FROM PortfolioPosition WHERE PortfolioId = 1)");
                ctx.Database.ExecuteSql($"DELETE FROM PortfolioPosition WHERE PortfolioId = 1");
            }

            var now = DateTime.UtcNow;
            var buy = new AddToPortfolioDto() { PortfolioId = 1, Amount = 5, Price = 10.11, PurchaseDate = now, StockTicker = "CVX" };
            var accessToken = await GetTestUserAccessToken();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(@"http://localhost:6001/api/portfolio"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(buy), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);

            var getReq = new HttpRequestMessage
            {
                RequestUri = new Uri(@"http://localhost:6001/api/portfolio"),
                Method = HttpMethod.Get,
            };
            getReq.Headers.Add("Authorization", "Bearer " + accessToken);
            getReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var portResp = await _httpClient.SendAsync(getReq);

            Assert.NotNull(portResp);
            Assert.True(portResp.IsSuccessStatusCode);

            var jsonString = await portResp.Content.ReadAsStringAsync();
            var portfolio = ExtendedJsonSerializer.Deserialize<PortfolioDto>(jsonString);

            Assert.NotNull(portfolio);
            Assert.True(portfolio.Positions.Count == 1);
            Assert.True(portfolio.Positions[0].Stock.Ticker == "CVX");
            Assert.True(portfolio.Positions[0].Buys.Count == 1);
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

    /*//TODO: This copied from client project, move to common
    internal class JwtToken
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public DateTime expires { get; set; }
    }*/
}