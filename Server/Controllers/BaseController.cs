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
using FinanceManager.Server.Database;

namespace Server.Controllers
{

    [Controller]
    public abstract class BaseController : ControllerBase
    {
        private IAuthService _authService;
        protected string UserId 
        {
            get 
            {
                return _authService.GetUserIdByName(User.Identity.Name);
            }
        }


        public BaseController(IAuthService authService)
        {
            _authService = authService;
        }


    }


}
