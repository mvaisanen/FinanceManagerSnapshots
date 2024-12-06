using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Dtos
{
    public class PortfolioDto
    {
        public PortfolioDto()
        {
            Positions = new List<PortfolioPositionDto>();
        }

        public int Id { get; set; }
        public virtual List<PortfolioPositionDto> Positions { get; set; }

        //public Dictionary<Currency, double> FxRates { get; set; } //TODO: Include later, or create new model "PortfolioWithRates" etc
    }


    public class PortfolioDtoEqualityComparer : IEqualityComparer<PortfolioDto>
    {
        public bool Equals(PortfolioDto x, PortfolioDto y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            if (x.Id != y.Id) return false;
            if (x.Positions == null && y.Positions == null) return true;
            if ((x.Positions == null && y.Positions != null) || (x.Positions != null && y.Positions == null)) return false;
            if (x.Positions.Count != y.Positions.Count) return false;

            foreach (var xPos in x.Positions)
            {
                if (!y.Positions.Any(yPos => new PortfolioPositionDtoEqualityComparer().Equals(yPos, xPos))) return false;
            }
            return true;
        }

        public int GetHashCode(PortfolioDto obj)
        {
            return $"{obj.Id}.{string.Concat(obj.Positions.Select(b => b.GetHashCode()))}".GetHashCode();
        }
    }
}
