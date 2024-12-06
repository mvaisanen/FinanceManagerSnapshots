using Common.Dtos;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Mappings
{
    public static class WatchlistMappings
    {
        public static WatchlistDTO ToDTO(this Watchlist w)
        {
            var dto = new WatchlistDTO();
            dto.Id = w.Id;
            dto.Name = w.Name;
            dto.Stocks = new List<WatchlistStockDTO>();
            foreach (var pos in w.WatchlistStocks)
            {
                var wlStockDto = pos.ToDTO();
                dto.Stocks.Add(wlStockDto);
            }
            return dto;
        }

        public static WatchlistStockDTO ToDTO(this WatchlistStock wls)
        {
            var dto = new WatchlistStockDTO();
            dto.Id = wls.Id;
            //dto.Stock = wls.Stock.ToDTO();
            dto.StockId = wls.Stock.Id;
            dto.TargetPrice = wls.TargetPrice;

            dto.CompanyName = wls.Stock.Name;
            dto.StockTicker = wls.Stock.Ticker;
            dto.CurrentPrice = wls.Stock.CurrentPrice;
            dto.EpsTtm = wls.Stock.EpsTtm;
            dto.Dividend = wls.Stock.Dividend;
            dto.Notify = wls.Notify;
            dto.RowVersion = wls.RowVersion;

            return dto;
        }
    }
}
