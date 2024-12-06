using Common.Dtos;
using Financemanager.Server.Database.Domain;

namespace Server.Mappings
{
    public static class HistoryMappings
    {
        public static HistoricalDividendDto ToDTO(this HistoricalDividend d)
        {
            var dto = new HistoricalDividendDto();
            dto.Id = d.Id;
            dto.StockId = d.StockId;
            dto.PaymentDate = d.PaymentdDate;
            dto.AmountPerShare = d.AmountPerShare;
            dto.ExDividendDate = d.ExDividendDate;

            return dto;
        }

        public static HistoricalPriceDto ToDTO(this HistoricalPrice d)
        {
            var dto = new HistoricalPriceDto();
            dto.Id = d.Id;
            dto.StockId = d.StockId;
            dto.Date = d.Date;
            dto.ClosePrice = d.ClosePrice;

            return dto;
        }
    }
}
