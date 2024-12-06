using Server.Auth.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Server.Auth.Services
{
    public interface IJwtHandler
    {
        //JsonWebToken Create(string username);
        JsonWebToken CreateAccessToken(IEnumerable<Claim> claims);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
