using FinanceManager.Server.Database;
using FinanceManager.Server.Database.Domain.Auth;
using System.Linq;
using System;

namespace Server.Services
{
    public interface IAuthService
    {
        public string GetUserIdByName(string username);
    }
    public class AuthService: IAuthService
    {
        private readonly AuthDbContext _authCtx;
        public AuthService(AuthDbContext authCtx) {
            _authCtx = authCtx;
        }

        public string GetUserIdByName(string username)
        {
            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == username);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }
            return user.Id;
        }
    }
}
