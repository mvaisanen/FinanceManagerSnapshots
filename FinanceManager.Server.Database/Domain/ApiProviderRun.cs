using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class ApiProviderRun
    {
        public int Id { get; set; }
        public string ProviderName { get; set; } //The API name used in the update, RapidApiYHFinance, IEX, etc
        //public DateTime ScheduledTime { get; set; }
        //public something UpdateFrequency { get; set; } //minute, hour, day, businessday, week, month, etc etc
        public DateTime TimestampUtc { get; set; }
        public int CreditsUsed { get; set; }

    }
}
