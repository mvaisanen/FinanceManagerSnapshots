using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class StockPurchaseDto
    {
        public int Id { get; set; }
        public virtual DateTime PurchaseDate { get; set; }
        public virtual double Amount { get; set; }
        public virtual double Price { get; set; }
    }

    public sealed class StockPurchaseDtoEqualityComparer : IEqualityComparer<StockPurchaseDto>
    {
        public bool Equals(StockPurchaseDto x, StockPurchaseDto y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            var equals = x.Id == y.Id && x.PurchaseDate == y.PurchaseDate && x.Amount == y.Amount && x.Price == y.Price;
            return equals;
        }

        public int GetHashCode(StockPurchaseDto obj)
        {
            return $"{obj.Id}.{obj.PurchaseDate}.{obj.Price}.{obj.Amount}".GetHashCode();
        }
    }
}
