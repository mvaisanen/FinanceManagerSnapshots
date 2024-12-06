using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Models
{
    public class Quote
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
        [JsonProperty("companyName")]
        public string CompanyName { get; set; }
        [JsonProperty("latestPrice")]
        public double? LatestPrice { get; set; }
        [JsonProperty("latestSource")]
        public string LatestSource { get; set; }

    }

}
