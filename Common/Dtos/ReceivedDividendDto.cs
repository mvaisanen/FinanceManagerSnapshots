using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class ReceivedDividendDTO
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string CompanyTicker { get; set; }
        public int? ShareCount { get; set; }
        public double? AmountPerShare { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Currency { get; set; }
        public double FxRate { get; set; }

        public double TotalReceived { get; set; }
    }
}
