using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Common.HelperModels
{
    public class AddToPortfolioDto
    {
        public AddToPortfolioDto()
        {

        }

        [Required]
        public int PortfolioId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Ticker can't be empty")]
        public string StockTicker { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [Required]
        [Range(1, Int32.MaxValue, ErrorMessage = "Minimum value is 1 stock")]
        public int Amount { get; set; }
        [Required]
        [Range(0.0001, Int32.MaxValue, ErrorMessage = "Minimum value is 0.0001")]
        public double Price { get; set; }


    }
}
