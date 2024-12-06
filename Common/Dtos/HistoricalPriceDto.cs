using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class HistoricalPriceDto
    {
        public int Id { get; set; }

        public int StockId { get; set; }

        public DateTime Date { get; set; }

        public double ClosePrice { get; set; } //Should be adjusted for splits etc, ie so that it is accurate at current time and comparable to dividend
    }
}
