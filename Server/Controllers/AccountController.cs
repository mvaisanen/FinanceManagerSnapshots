using FinanceManager.Server.Database;
using FinanceManager.Server.Database.Domain.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Auth.Entities;
using Server.Auth.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Controllers
{
   // [Route("[account]")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AuthDbContext _authDbContext;


        public AccountController(UserManager<AppUser> userManager, AuthDbContext authDbCtx)
        {
            _userManager = userManager;
            _authDbContext = authDbCtx;
        }


        [HttpPost("sign-up")]
        [AllowAnonymous]
        public async Task<IActionResult> SignUp([FromBody] SignUp request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var userIdentity = new AppUser() { UserName = request.Username };

            var result = await _userManager.CreateAsync(userIdentity, request.Password);

            if (!result.Succeeded) return new BadRequestObjectResult(result.Errors.First());

            // _accountService.SignUp(request.Username, request.Password);

            return new OkObjectResult("Account created");
        }

        
    }
}
