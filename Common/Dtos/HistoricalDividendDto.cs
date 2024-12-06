using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class HistoricalDividendDto
    {
        public int Id { get; set; }

        public int StockId { get; set; }

        public DateTime ExDividendDate { get; set; }

        public DateTime? PaymentDate { get; set; }

        public double AmountPerShare { get; set; } //Should be adjusted for splits etc, ie so that it is accurate at current time
    }
}
