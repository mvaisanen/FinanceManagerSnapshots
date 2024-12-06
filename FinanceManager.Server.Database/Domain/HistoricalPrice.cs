using System;
using System.ComponentModel.DataAnnotations;

namespace Financemanager.Server.Database.Domain
{
    public class HistoricalPrice
    {
        public int Id { get; set; }
        [Required]
        public int StockId { get; set; }
        public Stock Stock { get; set; }
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public double ClosePrice { get; set; } 

        public double ClosePriceAdjusted { get; set; } // No idea what this adjustment is (FMP data)
    }
}
