using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class WatchlistDTO
    {
        public WatchlistDTO()
        {
            Stocks = new List<WatchlistStockDTO>();
        }

        //[JsonProperty("Id")]
        public int Id { get; set; }

        public virtual string Name { get; set; }
        public virtual List<WatchlistStockDTO> Stocks { get; set; }
    }
}
