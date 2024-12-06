using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Dtos;
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
using Server.Mappings;
using FinanceManager.Server.Database;

namespace Server.Controllers
{
    [Route("api/fxrates")]
    [ApiExplorerSettings(GroupName = "Currency", IgnoreApi = false)]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private AuthDbContext _authCtx;
        //private IActorRef _currencyActor;
        private FxService _fxService;
        private ILogger _log;


        //public CurrencyController(CurrencyActorProvider cProvider, AuthDbContext authCtx)
        public CurrencyController(FxService fxSrv, AuthDbContext authCtx)
        {
            //_currencyActor = cProvider.Invoke();
            _fxService = fxSrv;
            _authCtx = authCtx;
            _log = LogManager.GetCurrentClassLogger();
        }

        [Route("current")]
        [HttpGet]
        public async Task<CurrencyRatesDto> GetCurrentRates()
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            //var res = await _currencyActor.Ask<DbOperationResult<CurrencyRatesDto>>(CurrencyMessage.NewGetCurrentRateIfAvailable(userId));
            return (await _fxService.GetCurrentRates()).ToDTO();
            /*if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);*/
        }
    }
}
