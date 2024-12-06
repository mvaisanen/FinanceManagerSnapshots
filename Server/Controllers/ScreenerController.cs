using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Dtos;
using FinanceManager.Server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server.Actors;
using Server.Models;
using Messages;
using Server.Services;
using Common.HelperModels;
using NLog;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "Screener", IgnoreApi = false)]
    [Authorize]
    public class ScreenerController : ControllerBase
    {
        private AuthDbContext _authCtx;
        private IActorRef _screenerActor;
        private ScreenerService _screenerService;
        private ILogger _log;


        public ScreenerController(ScreenerActorProvider pProvider, AuthDbContext authCtx, ScreenerService screenerService)
        {
            _screenerActor = pProvider.Invoke();
            _screenerService = screenerService;
            _authCtx = authCtx;
            _log = LogManager.GetCurrentClassLogger();
        }


        [HttpGet]
        public async Task<List<StockDTO>> GetChampions()
        {
            var champs = await _screenerActor.Ask<List<StockDTO>>(ScreenerMessage.NewGetAllChampions(true));
            return champs;
        }

        [HttpGet]
        [Route("search")]
        public async Task<List<StockDTO>> GetResults(double minCyoc=0, int minDgYears=0)
        {
            var res = await _screenerActor.Ask<List<StockDTO>>(ScreenerMessage.NewGetSearchResults(minCyoc, minDgYears));
            return res;
        }

        [HttpGet]
        [Route("searchbyenddivs")]
        public async Task<List<StockDTO>> SearchByEndDivs(double baseYield, double baseGrowth, int years, int mindgyears)
        {
            var res = await _screenerService.SearchByEndDividends(baseYield, baseGrowth, years, mindgyears);
            return res;
        }

    }


}
