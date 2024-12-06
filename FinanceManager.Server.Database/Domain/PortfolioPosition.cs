using Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace Financemanager.Server.Database.Domain
{
    public class PortfolioPosition
    {
        private PortfolioPosition() { /*Buys = new List<StockPurchase>();*/ }
        internal PortfolioPosition(Stock stock) { 
            Stock = stock;
            _buys = new HashSet<StockPurchase>();
        }

        public int PortfolioPositionId { get; private set; }
        [Timestamp]
        public byte[] RowVersion { get; private set; } = null!;
        public Stock Stock { get; private set; }
        public Portfolio Portfolio { get; private set; } = null!;


        public double TotalAmount
        {
            get { return Buys.Sum(b => b.Amount); }
        }

        //public  List<StockPurchase> Buys { get; set; }
        private HashSet<StockPurchase> _buys;
        public IReadOnlyCollection<StockPurchase> Buys => _buys?.ToList();

        internal void AddPurchase(DateTime purchaseDate, double amount, double price, Broker? broker, DbContext? context)
        {
            if (_buys == null)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context), "Context must be provided if Buys hasnt been loaded");
                context.Entry(this).Collection(pp => pp._buys).Load();
            }

            _buys.Add(new StockPurchase(purchaseDate, amount, price, broker));
        }

        internal void RemovePurchase(int purchaseId, DbContext? context)
        {
            if (_buys == null)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context), "Context must be provided if Buys hasnt been loaded");
                context.Entry(this).Collection(pp => pp._buys).Load();
            }

            var buyToRemove = _buys.SingleOrDefault(b => b.StockPurchaseId == purchaseId);
            if (buyToRemove != null)
            {
                _buys.Remove(buyToRemove);
            }
        }
    }
}
