using System;
using System.Collections.Generic;

namespace Server.Models
{

    public class FMPDividendHistoryResponse
    {
        public string symbol { get; set; }
        public List<HistoricalDiv> historical { get; set; }
    }

    public class HistoricalDiv
    {
        public string date { get; set; }
        public string label { get; set; }
        public double adjDividend { get; set; }
        public double dividend { get; set; }
        public string? recordDate { get; set; }
        public string? paymentDate { get; set; }
        public string? declarationDate { get; set; }
    }


}
