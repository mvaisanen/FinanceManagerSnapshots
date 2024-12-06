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
    public class Portfolio
    {
        private Portfolio(){ /*Positions = new List<PortfolioPosition>();*/ } //For EF Core
        public Portfolio(string userId)
        {
            UserId = userId;
            _positions = new HashSet<PortfolioPosition>();
        }

        public int Id { get; private set; }
        [Timestamp]
        public byte[] RowVersion { get; private set; } = null!;

        public string UserId { get; private set; }
        //public virtual List<PortfolioPosition> Positions { get; set; }
        private HashSet<PortfolioPosition> _positions;
        public IReadOnlyCollection<PortfolioPosition> Positions => _positions?.ToList();

        public void AddStockPurchase(Stock stock, DateTime purchaseDate, double amount, double price, Broker? broker = null, DbContext? context = null)
        {
            EnsurePositionsLoaded(context);

            var position = _positions.SingleOrDefault(p => p.Stock.Id == stock.Id);
            if (position == null)
            {
                position = new PortfolioPosition(stock);
                _positions.Add(position);
            }

            position.AddPurchase(purchaseDate, amount, price, broker, context); 
        }

        public void RemoveStockPurchase(int purchaseId, DbContext? context = null)
        {
            EnsurePositionsLoaded(context);

            var position = _positions.SingleOrDefault(p => p.Buys.Any(b => b.StockPurchaseId == purchaseId));
            if (position == null)
                throw new ArgumentException($"No positions found for given purchaseId {purchaseId}");

            position.RemovePurchase(purchaseId, context);
            if (position.Buys.Count == 0)
                _positions.Remove(position); //Removing last remaining buy from position removes the position itself
        }

        public void RemovePosition(int positionId, DbContext? context = null)
        {
            EnsurePositionsLoaded(context);

            _positions.RemoveWhere(p => p.PortfolioPositionId == positionId); //This should cascade-delete buys, but needs to be tested!
        }

        public void UpdatePurchase(int purchaseId, DateTime purchaseDate, double amount, double price, Broker? broker = null, DbContext? context = null)
        {
            EnsurePositionsLoaded(context);
            var purchase = _positions.SelectMany(pos => pos.Buys).SingleOrDefault(b => b.StockPurchaseId == purchaseId);
            if (purchase == null)
                throw new ArgumentException($"Invalid purchaseId (not found): {purchaseId}");
            purchase.UpdateData(purchaseDate, amount, price, broker);
        }

        private void EnsurePositionsLoaded(DbContext? context)
        {
            if (_positions == null)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context), "Context must be provided if Positions hasnt been loaded");
                context.Entry(this).Collection(p => p.Positions).Load();
            }
        }
    }
}
