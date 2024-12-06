using Common;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class ReceivedDividend
    {
        //[JsonProperty("Id")]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Symbol { get; set; } //Symbol as shown in various statements - usually same as CompanyTicker, but may differ -> cant determine company directly
        public string CompanyTicker { get; set; }
        public int? ShareCount { get; set; }
        public double? AmountPerShare { get; set; }
        public DateTime PaymentDate { get; set; }
        public Currency Currency { get; set; }
        public virtual Broker? Broker { get; set; }
        public double? FxRate { get; set; }
        public double TotalReceived { get; set; }

        public ReceivedDividend()
        {

        }

        public ReceivedDividend(string userId, string date, string ticker, string shareCount, string dividend, Currency currency, Broker broker, string fxRate=null)
        {
            UserId = userId;
            AmountPerShare = Double.Parse(dividend.Trim().Replace(" ", ""));
            CompanyTicker = ticker.Trim();
            //Currency = currency.Trim();
            Currency =  currency;
            Broker = broker;

            if (fxRate != null)
            {
                if (CultureInfo.CurrentCulture.EnglishName == "Finnish (Finland)")
                    FxRate = Double.Parse(fxRate.Trim().Replace(" ", ""), CultureInfo.CurrentCulture);
                else
                    FxRate = Double.Parse(fxRate.Trim().Replace(" ", "").Replace(',', '.')); //use dot as decimal separator
            }
            ShareCount = int.Parse(shareCount.Trim());
            PaymentDate = DateTime.ParseExact(date.Trim(), "d.M.yyyy", null);
            //TODO: Error handling! (currently not intended to be used with data read from files, only in db hardcoded initialization)
        }

        public ReceivedDividend(string userId, DateTime date, string ticker, string symbol, int? shareCount, double? amountPerShare, double total, Currency currency, 
             Broker broker, double? fxRate)
        {
            UserId = userId;
            
            CompanyTicker = ticker?.Trim();
            Symbol = symbol;
            //Currency = currency.Trim();  
            Currency =  currency;
            AmountPerShare = amountPerShare;
            TotalReceived = total;
            ShareCount = shareCount;
            PaymentDate = date;
            FxRate = fxRate;
            Broker = broker;
        }
    }
}
