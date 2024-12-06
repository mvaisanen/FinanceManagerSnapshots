using Common.Dtos;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Mappings
{
    public static class StockPurchaseMappings
    {
        public static StockPurchaseDto ToDTO(this StockPurchase sp)
        {
            var dto = new StockPurchaseDto();
            dto.Id = sp.StockPurchaseId;
            dto.Amount = sp.Amount;
            dto.Price = sp.Price;
            dto.PurchaseDate = sp.PurchaseDate;
            return dto;
        }

        /*public static StockPurchase ToBusinessObject(this StockPurchaseDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            var o = new StockPurchase(dto.Id);
            o.Amount = dto.Amount;
            o.Price = dto.Price;
            o.PurchaseDate = dto.PurchaseDate;
            return o;
        }*/
    }
}
