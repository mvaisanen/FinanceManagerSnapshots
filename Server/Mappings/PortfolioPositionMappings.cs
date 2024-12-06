using Common.Dtos;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Mappings
{
    public static class PortfolioPositionMappings
    {
        public static PortfolioPositionDto ToDTO(this PortfolioPosition p)
        {
            var dto = new PortfolioPositionDto();
            dto.Id = p.PortfolioPositionId;
            dto.Stock = p.Stock.ToDTO();
            dto.Buys = new List<StockPurchaseDto>();
            foreach (var buy in p.Buys)
            {
                var buyDto = buy.ToDTO();
                dto.Buys.Add(buyDto);
            }
            return dto;
        }
    }
}
