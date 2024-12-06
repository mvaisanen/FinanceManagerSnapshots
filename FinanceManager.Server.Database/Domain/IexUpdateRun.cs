using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class IexUpdateRun
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string UpdateType { get; set; }

        public int CreditsUsed { get; set; }

    }
}
