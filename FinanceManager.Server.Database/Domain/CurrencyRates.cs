using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class CurrencyRates
    {
        public int Id { get; private set; }
        public DateTime Date { get; set; }

        public bool NoData { get; set; } //True indicates ecb doesnt have data for this date (weekend, holiday, ...)

        public double EurUsd { get; set; }
        public double EurCad { get; set; }
        public double EurDkk { get; set; }
        public double EurGbp { get; set; }
        public double EurSek { get; set; }
        public double EurNok { get; set; }

        public double GetFxRate(Currency target, Currency source) //EURUSD -> Source = EUR, target = USD
        {
            if (target != Currency.EUR)
                throw new ArgumentException($"Target surrency not supported: {target.ToString()}"); //TODO: Other rates: UsdCad etc for public use, not just EUR for me ;p
            if (source == Currency.CAD) return EurCad;
            if (source == Currency.DKK) return EurDkk;
            if (source == Currency.USD) return EurUsd;
            throw new ArgumentException($"Currency conversion to {source.ToString()} not supported");
        }

    }
}
