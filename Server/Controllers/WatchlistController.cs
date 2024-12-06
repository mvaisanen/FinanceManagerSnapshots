using Akka.Actor;
using Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Dtos;
using Server.Actors;
using FinanceManager.Server.Database;
using Server.Models;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName ="Watchlist", IgnoreApi = false)]
    public class WatchlistController: ControllerBase
    {
        private IActorRef _watchlistActor;
        private AuthDbContext _authCtx;
        public WatchlistController(WatchlistActorProvider wlProvider, AuthDbContext authCtx)
        {
            _watchlistActor = wlProvider.Invoke();
            _authCtx = authCtx;
        }

        [Route("")]
        [Authorize]
        [HttpGet]
        public async Task<WatchlistDTO> WatchlistByUserId()
        {
            //var res = await _watchlistActor.Ask<DbOperationResult<WatchlistDTO>>(WatchlistMessage.NewGetWatchlist(1));
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                //return StatusCode(500, "Invalid User"); //should not happen since using authentication
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var res = await _watchlistActor.Ask<DbOperationResult<WatchlistDTO>>(WatchlistMessage.NewGetWatchlistByUserId(userId));
            if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);
        }

        [Route("{id}/stocks")]
        [HttpPost]
        [Authorize]
        public async Task<WatchlistDTO> AddOrUpdateWatchlistStock(int id, [FromBody]WatchlistStockDTO wlStock)
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                //return StatusCode(500, "Invalid User"); //should not happen since using authentication
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;
            
            var res = await _watchlistActor.Ask<DbOperationResult<WatchlistDTO>>(WatchlistMessage.NewAddOrUpdateStock(userId, id, wlStock));
            if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);
        }


        [Route("{id}/stocks/{wlsId}")]
        [HttpDelete]
        [Authorize]
        public async Task<WatchlistDTO> DeleteWatchlistStock(int id, int wlsId)
        {
            string userId = "";

            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                //return StatusCode(500, "Invalid User"); //should not happen since using authentication
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var res = await _watchlistActor.Ask<DbOperationResult<WatchlistDTO>>(WatchlistMessage.NewRemoveStockFromWatchlist(userId, id, wlsId));
            if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);
        }
    }
}
