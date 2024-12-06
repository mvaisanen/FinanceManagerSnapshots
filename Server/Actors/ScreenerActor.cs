using Akka.Actor;
using Akka.Event;
using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Server.Database;
using Microsoft.EntityFrameworkCore;
using Server.Mappings;
using Financemanager.Server.Database.Domain;
using Server.Models;
using Common.Dtos;
using FinanceManager.Server.Database;

namespace Server.Actors
{
    public class ScreenerActor : ReceiveActor
    {
        private ILoggingAdapter _log;
        IServiceScopeFactory _serviceScopeFactory;

        public ScreenerActor(IServiceScopeFactory serviceScopeFactory)
        {
            _log = Context.GetLogger();
            _log.Info("ScreenerActor started.");
            //_fxRef = FinanceManagerServer.FinanceManagerSystem.ActorSelection("/user/fxrates");

            //System.Diagnostics.Debug.WriteLine("PortfolioActor startup...");
            _serviceScopeFactory = serviceScopeFactory;
            Become(Ready);
        }

        private void Ready()
        {

            Receive<ScreenerMessage.GetAllChampions>(msg =>
            {
                List<StockDTO> results = new List<StockDTO>();
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    //_log.Debug($"3.5/6.5 TotalYoC20: {CumulativeYoCIn20Y(3.5, 6.5)}");
                    var champions = ctx.Stocks.Where(s => s.DGYears >= 25).ToList();
                    foreach (var champ in champions)
                    {
                        //_log.Debug($"{champ.Ticker} TotalYoC20: {CumulativeYoCIn20Y(champ.Yield, champ.DivGrowth1)}");
                        var dto = champ.ToDTO();
                        results.Add(dto);
                    }
                }
                Sender.Tell(results);
            });

            Receive<ScreenerMessage.GetSearchResults>(msg =>
            {
                List<StockDTO> results = new List<StockDTO>();
                var minCyoc = msg.minCyoc;
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var res = ctx.Stocks.Where(s => s.DGYears >= msg.dgyears).ToList().Where(s => CumulativeYoCIn20Y(s.Yield, estimateGrowth(s.DivGrowth1, s.DivGrowth3, s.DivGrowth5, s.DivGrowth10)) >= minCyoc).ToList();
                    foreach (var champ in res)
                    {
                        var dto = champ.ToDTO();
                        results.Add(dto);
                    }
                }
                Sender.Tell(results);
            });
        }



        private double CumulativeYoCIn20Y(double divYield, double divGrowth)
        {
            var initYield = divYield / 100.0;
            var driYield = divYield / 100.0;
            var years = 21;
            var growthRates = new double[22];
            for (var i = 0; i <= years; i++)
                growthRates[i] = 1.0 + divGrowth / 100.0;

            var divsPerYear = new double[years + 1];
            var divTable = new double[years + 1, years + 1]; //2-dim, year=1st dim, dri=2nd dim
            var initialInvestment = 100;
            divTable[0, 0] = initYield * initialInvestment; //Year 1, DRI 0. That is, dividends received at end of year 0
            divsPerYear[0] = divTable[0, 0];

            //dri0 special handling
            for (var y = 1; y < years; y++)
            {
                divTable[y, 0] = divTable[y - 1, 0] * growthRates[y - 1];
            }


            for (var year = 1; year <= years; year++)
            {
                for (var dri = 1; dri <= year - 1; dri++)
                {
                    var res = divsPerYear[dri - 1] * driYield * Math.Pow(growthRates[year - 1], year - (dri + 1));
                    divTable[year - 1, dri] = res;
                }
                var divsThisYear = 0.0;
                for (var d = 0; d <= years - 1; d++)
                {
                    divsThisYear = divsThisYear + divTable[year - 1, d];
                }
                divsPerYear[year - 1] = divsThisYear;
            }

            return divsPerYear[years - 1];
        }


        private double estimateGrowth(double dgr1, double dgr3, double dgr5, double dgr10)
        {
            return new List<double>() { dgr1, dgr3, dgr5, dgr10 }.Min();
        }

    }
}
