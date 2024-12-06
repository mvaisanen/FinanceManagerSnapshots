using System;
using System.ComponentModel.DataAnnotations;

namespace Financemanager.Server.Database.Domain
{
    public class HistoricalDividend
    {
        public int Id { get; set; }

        public int StockId { get; set; }
        [Required]
        public DateTime ExDividendDate { get; set; }
        
        public DateTime? PaymentdDate { get; set; }
        [Required]
        public double AmountPerShare { get; set; } //Should be adjusted for splits etc, ie so that it is accurate at current time
    }
}
