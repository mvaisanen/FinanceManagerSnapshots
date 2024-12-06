using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace Financemanager.Server.Database.Domain
{
    public class StockDataUpdateSource
    {
        public int Id { get; private set; }
        public int StockId { get; private set; }
        public StockAttribute Attribute { get; private set; }
        public DataUpdateSource  Source { get; private set; }
        public DateTime LastUpdated { get; private set; }

        public StockDataUpdateSource(StockAttribute attribute, DataUpdateSource source)
        {
            Attribute = attribute;
            Source = source;
        }
        /// <summary>
        /// Update LastUpdated time. This and other Stock related classes should be in their own project, so internal limits 
        /// access even more (only from same assembly)
        /// </summary>
        /// <param name="lastUpdated"></param>
        internal void UpdateLastUpdated(DateTime lastUpdated)
        {
            LastUpdated = lastUpdated;
        }
    }
}
