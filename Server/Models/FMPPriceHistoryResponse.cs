using System;
using System.Collections.Generic;

namespace Server.Models
{

    public class FMPPriceHistoryResponse
    {
        public string symbol { get; set; }
        public List<StockHistoricalPrice> historical { get; set; }
    }

    public class StockHistoricalPrice
    {
        public string date { get; set; }
        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public double adjClose { get; set; }
        public int volume { get; set; }
        public int unadjustedVolume { get; set; }
        public double change { get; set; }
        public double changePercent { get; set; }
        public double vwap { get; set; }
        public string label { get; set; }
        public double changeOverTime { get; set; }
    }


}
