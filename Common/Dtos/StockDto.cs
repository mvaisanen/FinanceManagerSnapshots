using System;

namespace Common.Dtos
{
    public class StockDTO
    {
        //[JsonProperty("Id")]
        public int Id { get; set; }
        public virtual string Ticker { get; set; }

        public virtual double CurrentPrice { get; set; }
        public virtual DateTime? PriceLastUpdated { get; set; }
        //public virtual DateTime? DataLastUpdated { get; set; }
        public virtual string Name { get; set; }

        public virtual string Sector { get; set; }

        public virtual double Dividend { get; set; }

        public virtual double Yield
        {
            get { return Dividend != 0 ? Dividend / CurrentPrice * 100 : 0.0; }
        }

        /*public virtual double Coverage
        {
            get { return Dividend / EpsTtm * 100; }
        }*/

        public virtual int DGYears { get; set; }

        public virtual double EpsTtm { get; set; }

        /*public virtual double PE
        {
            get { return CurrentPrice / EpsTtm; }
        }*/
        public virtual int? SSDSafetyScore { get; set; }
        public virtual DateTime? SSDScoreDate { get; set; }

        public virtual double DivGrowth1 { get; set; }

        public virtual double DivGrowth3 { get; set; }

        public virtual double DivGrowth5 { get; set; }

        public virtual double DivGrowth10 { get; set; }

        public virtual double EpsGrowth5 { get; set; }

        //TODO: currency / update source
        //public virtual UpdateSource PriceSource { get; set; }

        public virtual Currency Currency { get; set; }
        //public virtual PriceSource PriceSource { get; set; }
        //public virtual DataUpdateSource DataUpdateSource { get; set; }
    }
}
