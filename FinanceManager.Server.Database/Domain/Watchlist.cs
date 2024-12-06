using Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class Watchlist
    {
        public Watchlist()
        {
            _watchlistStocks = new HashSet<WatchlistStock>();
        }

        public Watchlist(string userId)
        {
            UserId = userId;
            _watchlistStocks = new HashSet<WatchlistStock>();
        }

        public int Id { get; private set; }
        [Timestamp]
        public byte[] RowVersion { get; private set; } = null!;
        public string UserId { get; private set; }
        public virtual string Name { get; private set; }

        private HashSet<WatchlistStock> _watchlistStocks;
        public IReadOnlyCollection<WatchlistStock> WatchlistStocks => _watchlistStocks?.ToList();

        public void AddOrUpdateStock(Stock stock, double? targetPrice=null, bool notify=false, DbContext? context = null)
        {
            EnsurePositionsLoaded(context);

            var wlStock = _watchlistStocks.SingleOrDefault(p => p.Stock.Id == stock.Id);
            if (wlStock == null)
            {
                wlStock = new WatchlistStock() { Stock = stock, TargetPrice = targetPrice, Notify = notify };
                _watchlistStocks.Add(wlStock);
            }
            wlStock.AlarmSent = false; //Update removes sent flag           
        }

        public void RemoveFromWatchlist(int watchlistStockId, DbContext? context = null)
        {
            EnsurePositionsLoaded(context);

            var wlStock = _watchlistStocks.SingleOrDefault(p => p.Id == watchlistStockId);
            if (wlStock == null)
                throw new ArgumentException($"Watchlist stock with id {watchlistStockId} not found");
            _watchlistStocks.Remove(wlStock);
        }

        private void EnsurePositionsLoaded(DbContext? context)
        {
            if (_watchlistStocks == null)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context), "Context must be provided if WatchlistStocks hasnt been loaded");
                context.Entry(this).Collection(p => p.WatchlistStocks).Load();
            }
        }
    }
}
