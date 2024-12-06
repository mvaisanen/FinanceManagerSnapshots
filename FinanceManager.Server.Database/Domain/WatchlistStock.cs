using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class WatchlistStock
    {
        public int Id { get; private set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }

        public virtual Stock Stock { get; set; }
        public virtual double? TargetPrice { get; set; }
        public virtual bool Notify { get; set; }
        public virtual bool AlarmSent { get; set; }
    }
}
