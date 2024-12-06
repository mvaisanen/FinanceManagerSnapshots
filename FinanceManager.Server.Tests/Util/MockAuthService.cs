using Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.Tests.Util
{
    internal class MockAuthService : IAuthService
    {
        private string _userId;
        public MockAuthService(string userId)
        {
            _userId = userId;
        }

        public string GetUserIdByName(string username)
        {
            return _userId;
        }
    }
}
