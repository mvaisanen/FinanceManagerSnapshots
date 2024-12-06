using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class WatchlistStockDTO
    {
        //[JsonProperty("Id")]
        public int Id { get; set; }
        //public virtual StockDTO Stock { get; set; }
        public virtual int StockId { get; set; }
        public byte[] RowVersion { get; set; }

        //[DisplayFormat(ConvertEmptyStringToNull = true, ApplyFormatInEditMode = true, DataFormatString = "N2")]
        public virtual double? TargetPrice { get; set; }
        public virtual bool Notify { get; set; }

        //Below ones meant for informationional purposes only - dont update these to db when received from clients!
        public virtual string StockTicker { get; set; }
        public virtual string CompanyName { get; set; }
        public virtual double CurrentPrice { get; set; }
        public virtual double EpsTtm { get; set; }
        public virtual double Dividend { get; set; }
    }
}
