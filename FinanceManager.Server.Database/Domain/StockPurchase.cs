using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace Financemanager.Server.Database.Domain
{
    public class StockPurchase
    {
        private StockPurchase() { }

        internal StockPurchase(DateTime purchaseDate, double amount, double price, Broker? broker = null)
        {
            UpdateData(purchaseDate, amount, price, broker);    
        }
        
        public int StockPurchaseId { get; private set; }
        [Timestamp]
        public byte[] RowVersion { get; private set; } = null!;
        public DateTime PurchaseDate { get; private set; }
        public double Amount { get; private set; }
        public double Price { get; private set; }
        public Broker? Broker { get; private set; }

        internal void UpdateData(DateTime purchaseDate, double amount, double price, Broker? broker = null)
        {
            PurchaseDate = purchaseDate;
            Amount = amount;
            Price = price;
            Broker = broker;
        }
    }
}
