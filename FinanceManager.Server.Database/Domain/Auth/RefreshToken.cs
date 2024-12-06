using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.Database.Domain.Auth
{
    public class RefreshToken
    {
        public int Id { get; private set; }
        public string Username { get; set; }
        public string Token { get; set; }
        public bool Revoked { get; set; }
        public DateTime Expires { get; set; }
    }
}
