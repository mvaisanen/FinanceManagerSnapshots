using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common
{
    public class JwtToken
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public DateTime expires { get; set; }
    }
}
