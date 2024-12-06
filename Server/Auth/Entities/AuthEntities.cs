using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Auth.Entities
{


    /*public class RefreshToken
    {
        public int Id { get; private set; }
        public string Username { get; set; }
        public string Token { get; set; }
        public bool Revoked { get; set; }
        public DateTime Expires { get; set; }
    }*/

    public class JsonWebToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expires { get; set; }
    }
}
