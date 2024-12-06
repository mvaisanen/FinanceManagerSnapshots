using Common.Dtos;
using NLog;
using Server.Mappings;
using Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceManager.Server.Database;
using Financemanager.Server.Database.Domain;

namespace Server.Services
{
    public class ScreenerService: IDisposable
    {
        private readonly FinanceManagerContext _ctx;
        private Logger _log;
        public readonly int InstanceNumber;
        private static int InstancesCreated;

        public ScreenerService(FinanceManagerContext ctx)
        {
            _log = LogManager.GetCurrentClassLogger();
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);
            _log.Debug($"ScreenerService #{InstanceNumber} created with FinanceManagerContext instance #{ctx.InstanceNumber}");
            _ctx = ctx;
        }

        public async Task<List<StockDTO>> SearchByEndDividends(double baseYield, double baseGrowth, int years, int mindgyears)
        {
            List<StockDTO> results = new List<StockDTO>();

            var referenceDivs = DivMath.CalculateEndDividends(100, baseYield, baseGrowth, years);
            _log.Debug($"SearchByEndDividends(): baseYield={baseYield}, baseGrowth={baseGrowth}, years={years} -> divs={referenceDivs}");

            //Note: This filtering first has to fetch all stocks from db, so definitely not very efficient
            //var res = _ctx.Stocks.ToList()
            //    .Where(s => DivMath.CalculateEndDividends(100, minGrowth(s.DivGrowth1, s.DivGrowth3, s.DivGrowth5, s.DivGrowth10), 0, s.Yield, minGrowth(s.DivGrowth1, s.DivGrowth3, s.DivGrowth5, s.DivGrowth10), years) >= referenceDivs).ToList();

            List<Stock> res = new List<Stock>();
            foreach (var s in _ctx.Stocks.Where(s => s.DGYears >= mindgyears).ToList())
            {
                var thisStockGrowth = minGrowth(s.DivGrowth1, s.DivGrowth3, s.DivGrowth5, s.DivGrowth10);
                var endDivs = DivMath.CalculateEndDividends(100, s.Yield, thisStockGrowth, years);
                if (endDivs >= referenceDivs)
                {
                    _log.Debug($"Stock {s.Ticker} yield={s.Yield}, growth={thisStockGrowth} -> divs={endDivs}, adding to results");
                    res.Add(s);
                }
            }

            foreach (var champ in res)
            {
                var dto = champ.ToDTO();
                results.Add(dto);
            }

            return results;
        }

        private double minGrowth(double dgr1, double dgr3, double dgr5, double dgr10)
        {
            return new List<double>() { dgr1, dgr3, dgr5, dgr10 }.Min();
        }

        public async void Dispose()
        {
            await _ctx.DisposeAsync();
        }
    }
}
