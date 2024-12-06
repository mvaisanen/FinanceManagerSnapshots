using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Server.Models
{
    public class AlphaVantageStockQuoteResponse
    {
        [JsonPropertyName("Global Quote")]
        public GlobalQuote GlobalQuote { get; set; }
    }

    public class GlobalQuote
    {
        [JsonPropertyName("01. symbol")]
        public string _01Symbol { get; set; }

        [JsonPropertyName("02. open")]
        public string _02Open { get; set; }

        [JsonPropertyName("03. high")]
        public string _03High { get; set; }

        [JsonPropertyName("04. low")]
        public string _04Low { get; set; }

        [JsonPropertyName("05. price")]
        public string _05Price { get; set; }

        [JsonPropertyName("06. volume")]
        public string _06Volume { get; set; }

        [JsonPropertyName("07. latest trading day")]
        public string _07LatestTradingDay { get; set; }

        [JsonPropertyName("08. previous close")]
        public string _08PreviousClose { get; set; }

        [JsonPropertyName("09. change")]
        public string _09Change { get; set; }

        [JsonPropertyName("10. change percent")]
        public string _10ChangePercent { get; set; }
    }
}
