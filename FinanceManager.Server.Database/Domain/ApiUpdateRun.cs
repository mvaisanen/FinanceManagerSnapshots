using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class ApiUpdateRun
    {
        public int Id { get; set; }
        public string UpdateType { get; set; } //TODO: Id-reference to a type in database?
        //public DateTime ScheduledTime { get; set; }
        //public something UpdateFrequency { get; set; } //minute, hour, day, businessday, week, month, etc etc
        public DateTime TimestampUtc { get; set; }
        //public int CreditsUsed { get; set; } //not needed, as this is a client/provider thing, not directly related to certain update

    }
}
