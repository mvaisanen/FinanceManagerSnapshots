using Akka.Actor;
using Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Collections;
using Common.Dtos;
using Common.HelperModels;
using NLog;
using Server.Actors;
using Server.Models;
using Server.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinanceManager.Server.Database;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "Dividends", IgnoreApi = false)]
    [Authorize]
    public class DividendsController: ControllerBase
    {
        private IActorRef _dividendActor;
        private AuthDbContext _authCtx;
        private DividendService _divService;
        private ILogger _log;

        public DividendsController(DividendActorProvider divProvider, DividendService divService, AuthDbContext authCtx)
        {
            _log = LogManager.GetCurrentClassLogger();
            _dividendActor = divProvider.Invoke();
            _divService = divService;
            _authCtx = authCtx;
        }

        [Route("")]       
        [HttpGet]
        public async Task<List<ReceivedDividendDTO>> DividendsByUserId()
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var res = await _dividendActor.Ask<DbOperationResult<List<ReceivedDividendDTO>>>(DividendMessage.NewGetDividendsByUserId(userId));
            if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);
        }

        [Route("ttm")]
        [HttpGet]
        public async Task<List<ReceivedDividendDTO>> TtmDividends()
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var res = await _dividendActor.Ask<DbOperationResult<List<ReceivedDividendDTO>>>(DividendMessage.NewGetTtmDividends(userId));
            if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);
        }

        [Route("")]
        [HttpDelete]
        public async Task<List<ReceivedDividendDTO>> DeleteDividendPayment(int divId)
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var res = await _dividendActor.Ask<DbOperationResult<List<ReceivedDividendDTO>>>(DividendMessage.NewDeleteReceivedDividend(userId, divId));
            if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);
        }

        [HttpPost]
        [Route("uploadNordnetCsv")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<DividendsChangedDto> ProcessNordnetCsv([FromBody] FileUpload fileUpload)
        {
            string userId = "";
            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                _log.Debug($"User was not found! From context: User={User.Identity.Name}");
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            return await _divService.UploadUsersNordnetDividends(userId, fileUpload);
        }

        [HttpPost]
        [Route("uploadIBXml")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<string> ProcessIBXml(IFormFile file)
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var result = string.Empty;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                result = reader.ReadToEnd();
            }
            var lines = result.Split('\n').ToList();

            var res = await _dividendActor.Ask<DbOperationResult<bool>>(DividendMessage.NewProcessIBXml(ListModule.OfSeq(lines), userId));

            if (res.Succeeded)
            {
                return "Csv processing successful";
            }
            throw new AkkaError(500, res.ErrorMsg);
        }


        [HttpPost]
        [Route("uploadibcsv")]
        public async Task<DividendsChangedDto> UploadIbDividendsCsv([FromBody] FileUpload fileUpload, bool multiaccount)
        {
            string userId = "";
            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                _log.Debug($"User was not found! From context: User={User.Identity.Name}");
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            return await _divService.UploadUsersIbDividends(userId, multiaccount, fileUpload);
        }


        [HttpPost]
        [Route("uploadGeneralCsv")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<string> UploadGeneralCsv([FromBody]string[] lines)
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var res = await _dividendActor.Ask<DbOperationResult<bool>>(DividendMessage.NewProcessGeneralCsv(ListModule.OfSeq(lines), userId));

            if (res.Succeeded)
            {
                return "Csv processing successful";
            }
            throw new AkkaError(500, res.ErrorMsg);
        }


        [HttpGet]
        [Route("plans")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<DividendsPlanDto> GetPlan()
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            return await _divService.GetUsersDividendPlan(userId);
        }

        [HttpPost]
        [Route("plans")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<DividendsPlanDto> AddOrUpdatePlan([FromBody] DividendsPlanDto planDto)
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            return await _divService.SaveDividendPlan(planDto, userId);
        }
    }
}
