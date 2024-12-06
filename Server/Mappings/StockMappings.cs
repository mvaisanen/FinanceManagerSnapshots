using Common.Dtos;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Mappings
{
    public static class StockMappings
    {
        public static StockDTO ToDTO(this Stock s)
        {
            var dto = new StockDTO();
            dto.Currency = s.Currency;
            dto.CurrentPrice = s.CurrentPrice;
            //dto.DataLastUpdated = s.DataLastUpdated;
            dto.DGYears = s.DGYears;
            dto.SSDSafetyScore = s.SSDSafetyScore != null ? s.SSDSafetyScore.DivSafetyScore : null;
            dto.SSDScoreDate = s.SSDSafetyScore != null ? s.SSDSafetyScore.ScoreDate : null;
            dto.DivGrowth1 = s.DivGrowth1;
            dto.DivGrowth3 = s.DivGrowth3;
            dto.DivGrowth5 = s.DivGrowth5;
            dto.DivGrowth10 = s.DivGrowth10;
            dto.Dividend = s.Dividend;
            dto.EpsGrowth5 = s.EpsGrowth5;
            dto.EpsTtm = s.EpsTtm;
            dto.Id = s.Id;
            dto.Sector = s.Sector;
            dto.Name = s.Name;
            dto.PriceLastUpdated = s.StockDataUpdateSources?.SingleOrDefault(d => d.Attribute == Common.StockAttribute.Price)?.LastUpdated;
            dto.Ticker = s.Ticker;

            return dto;
        }
    }
}
