using Common.Dtos;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Mappings
{
    public static class DividendMappings
    {
        public static ReceivedDividendDTO ToDTO(this ReceivedDividend d, double? externalRate=null)
        {
            if (d.FxRate == null && externalRate == null)
                throw new ArgumentException("Dividend missing fx rate");

            var dto = new ReceivedDividendDTO();
            dto.Id = d.Id;
            dto.AmountPerShare = d.AmountPerShare;
            dto.Symbol = d.Symbol;
            dto.CompanyTicker = d.CompanyTicker;
            dto.Currency = d.Currency.ToString();
            if (externalRate == null)
                dto.FxRate = (double)d.FxRate;
            else
                dto.FxRate = (double)externalRate;
            dto.ShareCount = d.ShareCount;
            dto.PaymentDate = d.PaymentDate;
            dto.TotalReceived = d.TotalReceived;

            return dto;
        }

        public static DividendsPlanDto ToDTO(this DividendsPlan p)
        {
            var dto = new DividendsPlanDto();
            dto.NewDivGrowth = p.NewDivGrowth;
            dto.CurrentDivGrowth = p.CurrentDivGrowth;
            dto.StartDividends = p.StartDividends;
            dto.StartYear = p.StartYear;
            dto.YearlyInvestment = p.YearlyInvestment;
            dto.Years = p.Years;
            dto.Yield = p.Yield;

            return dto;
        }
    }
}
