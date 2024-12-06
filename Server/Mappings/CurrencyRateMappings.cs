using Common.Dtos;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Mappings
{
    public static class CurrencyRateMappings
    {
        public static CurrencyRatesDto ToDTO(this CurrencyRates r)
        {
            var dto = new CurrencyRatesDto();
            dto.Date = r.Date;
            dto.EurCad = r.EurCad;
            dto.EurDkk = r.EurDkk;
            dto.EurGbp = r.EurGbp;
            dto.EurNok = r.EurNok;
            dto.EurSek = r.EurSek;
            dto.EurUsd = r.EurUsd;
            
            return dto;
        }
    }
}
