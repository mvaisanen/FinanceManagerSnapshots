using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Dtos
{
    public class PortfolioPositionDto
    {
        public PortfolioPositionDto()
        {
            Buys = new List<StockPurchaseDto>();
        }

        public int Id { get; set; }
        public virtual StockDTO Stock { get; set; }


        public double TotalAmount
        {
            get { return Buys.Sum(b => b.Amount); }
        }

        public virtual List<StockPurchaseDto> Buys { get; set; }
    }

    public  class PortfolioPositionDtoEqualityComparer : IEqualityComparer<PortfolioPositionDto>
    {
        public bool Equals(PortfolioPositionDto x, PortfolioPositionDto y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            if (x.Stock.Id != y.Stock.Id) return false; //TODO: Better stock comparer
            if (x.Buys == null && y.Buys == null) return true;
            if ((x.Buys == null && y.Buys != null) || (x.Buys != null && y.Buys == null)) return false;
            if (x.Buys.Count != y.Buys.Count) return false;
            foreach (var xBuy in x.Buys)
            {
                if (!y.Buys.Any(yBuy => new StockPurchaseDtoEqualityComparer().Equals(yBuy, xBuy))) return false;
            }
            return true;
        }

        public int GetHashCode(PortfolioPositionDto obj)
        {
            //TODO: Better comparer for Stock, now just id
            return $"{obj.Id}.{obj.Stock.Id}.{string.Concat(obj.Buys.Select(b => b.GetHashCode()))}".GetHashCode();
        }
    }
}
