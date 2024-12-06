using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class CurrencyRatesDto
    {
        public DateTime Date { get; set; }

        public double EurUsd { get; set; }
        public double EurCad { get; set; }
        public double EurDkk { get; set; }
        public double EurGbp { get; set; }
        public double EurSek { get; set; }
        public double EurNok { get; set; }


        public double GetFxRate(Currency target, Currency source) // if stock price is in usd and we want to get it in euros, target is EUR, source USD
        {
            if (target != Currency.EUR)
                throw new ArgumentException($"Target currency not supported: {target.ToString()}"); //TODO: Other rates: UsdCad etc for public use, not just EUR for me ;p
            if (target == source) return 1.0000;
            if (source == Currency.CAD) return EurCad;
            if (source == Currency.DKK) return EurDkk;
            if (source == Currency.USD) return EurUsd;
            throw new ArgumentException($"Currency conversion from {target} to {source.ToString()} not supported");
        }
    }
}
