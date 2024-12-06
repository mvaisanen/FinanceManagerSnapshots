using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceManager.Server.Database.Domain.Auth
{
    public class AppUser : IdentityUser
    {
        //public int Id { get; set; }
        // public string Password { get; set; }
        public virtual List<RefreshToken> RefreshTokens { get; set; }// = new List<RefreshToken>();

        public AppUser()
        {
            RefreshTokens = new List<RefreshToken>();
        }
    }
}
