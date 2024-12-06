using FinanceManager.Server.Database;
using FinanceManager.Server.Database.Domain.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Auth.Entities;
using Server.Auth.Services;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using static Server.Auth.Entities.ClaimConstants;

namespace Server.Controllers
{
    public class AuthController: Controller
    {
        // Partly based on https://github.com/ruidfigueiredo/RefreshTokensWebApiExample
        // and https://piotrgankiewicz.com/2017/12/07/jwt-refresh-tokens-and-net-core/

        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtHandler _jwtHandler;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly AuthDbContext _authDbCtx;

        public AuthController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IJwtHandler jwtHandler, IPasswordHasher<AppUser> passwordHasher, AuthDbContext authDbCtx/*, IJwtFactory jwtFactory, IOptions<JwtIssuerOptions> jwtOptions*/)
        {
            _userManager = userManager;
            _jwtHandler = jwtHandler;
            _passwordHasher = passwordHasher;
            _authDbCtx = authDbCtx;
            _roleManager = roleManager;
            // _jwtFactory = jwtFactory;
            // _jwtOptions = jwtOptions.Value;
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]SignIn credentials)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var identity = await GetClaimsIdentity(credentials.Username, credentials.Password);
            if (identity == null)
            {
                throw new Exception("Invalid credentials.");
            }

            var user = await _userManager.FindByNameAsync(credentials.Username);

            var jwt = _jwtHandler.CreateAccessToken(identity.Claims);
            var refreshToken = _jwtHandler.GenerateRefreshToken();
            jwt.RefreshToken = refreshToken;

            RefreshToken RefToken = new RefreshToken() { Username = credentials.Username, Token = refreshToken, Revoked=false, Expires = DateTime.UtcNow.AddMinutes(5) };

            _authDbCtx.RefreshTokens.Add(RefToken);
            await _authDbCtx.SaveChangesAsync();
            user.RefreshTokens.Add(RefToken);
            await _userManager.UpdateAsync(user);

            return new OkObjectResult(jwt);
        }

        // POST api/auth/token
        [HttpPost("token")]
        public async Task<IActionResult> Refresh(string token, string refreshToken) //get token+refresh token using refresh token
        {
            var principal = _jwtHandler.GetPrincipalFromExpiredToken(token);
            var username = principal.Identity.Name; //this is mapped to the Name claim by default

            var user = _authDbCtx.Users.Include(us => us.RefreshTokens).SingleOrDefault(u => u.UserName == username);
            var matchingRefreshToken = user.RefreshTokens.FirstOrDefault(r => r.Token == refreshToken);
            //if (user == null || matchingRefreshToken == null) return BadRequest();
            if (user == null || matchingRefreshToken == null) throw new UnauthorizedError("Invalid credentials for refresh");
            if (matchingRefreshToken.Expires < DateTime.UtcNow) throw new UnauthorizedError("RefreshToken expired");

            var jwt = _jwtHandler.CreateAccessToken(principal.Claims); //huono... paranna
            var newRefreshToken = _jwtHandler.GenerateRefreshToken();

            RefreshToken RefToken = new RefreshToken() { Username = username, Token = newRefreshToken, Revoked = false, Expires = DateTime.UtcNow.AddMinutes(5) };

            _authDbCtx.RefreshTokens.Remove(matchingRefreshToken);
            _authDbCtx.RefreshTokens.Add(RefToken);
            await _authDbCtx.SaveChangesAsync();

            user.RefreshTokens.Remove(matchingRefreshToken);
            user.RefreshTokens.Add(RefToken);
            await _userManager.UpdateAsync(user);

            jwt.RefreshToken = RefToken.Token;

            return new OkObjectResult(jwt);
        }

        [HttpPost("logout"), Authorize]
        public async Task<IActionResult> Revoke()
        {
            var username = User.Identity.Name;

            var user = _authDbCtx.Users.SingleOrDefault(u => u.UserName == username);
            if (user == null) return BadRequest();

            user.RefreshTokens.Clear();
            await _userManager.UpdateAsync(user);

            var usersRefreshTokens = _authDbCtx.RefreshTokens.Where(r => r.Username == username);
            _authDbCtx.RefreshTokens.RemoveRange(usersRefreshTokens);
            await _authDbCtx.SaveChangesAsync();

            return NoContent();
        }


        private async Task<ClaimsIdentity> GetClaimsIdentity(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return await Task.FromResult<ClaimsIdentity>(null);

            // get the user to verifty
            var userToVerify = await _userManager.FindByNameAsync(userName);

            if (userToVerify == null) return await Task.FromResult<ClaimsIdentity>(null);

            // check the credentials
            if (await _userManager.CheckPasswordAsync(userToVerify, password))
            {
                return await Task.FromResult(GenerateClaimsIdentity(userToVerify, userToVerify.Id)); //Menee nyt kaikille? -> Googleta miten tietokannasta tms
            }

            // Credentials are invalid, or account doesn't exist
            return await Task.FromResult<ClaimsIdentity>(null);
        }

        public ClaimsIdentity GenerateClaimsIdentity(AppUser user, string id)
        {
            var isAdmin = _userManager.IsInRoleAsync(user, "Admin").Result;
            var roles = _userManager.GetRolesAsync(user).Result;
            var roleClaims = roles.Select(r => new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", r)).ToList();
            roleClaims.Add(new Claim(JwtClaimIdentifiers.Id, id));
            return new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), roleClaims.ToArray());

            /*
            return new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), new[]
            {
                new Claim(JwtClaimIdentifiers.Id, id),
                new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", isAdmin ? "Admin" : "User")
                //new Claim(JwtClaimIdentifiers.Rol, isAdmin ? "Admin" : "User")
            });*/
        }
    }
}
