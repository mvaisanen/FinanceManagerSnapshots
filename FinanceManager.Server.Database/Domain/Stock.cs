using Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace Financemanager.Server.Database.Domain
{
    public class Stock
    {
        public int Id { get; private set; }
        [Timestamp]
        public byte[] RowVersion { get; private set; } = null!;
        public string Ticker { get; private set; }
        public  double CurrentPrice { get; private set; }
        //public  DateTime? PriceLastUpdated { get; private set; }
        //public  DateTime? DataLastUpdated { get; set; }
        public string Name { get; private set; }
        public string? Sector { get; private set; }
        public double Dividend { get; private set; }
        public int DGYears { get; private set; }
        public double EpsTtm { get; private set; }
        public SSDScore? SSDSafetyScore { get; set; }
        public double DivGrowth1 { get; private set; }
        public double DivGrowth3 { get; private set; }
        public double DivGrowth5 { get; private set; }
        public double DivGrowth10 { get; private set; }
        public double EpsGrowth5 { get; private set; }
        public double DeptToEquity { get; private set; }
        //public PriceSource PriceSource { get; set; }
        //public DataUpdateSource DataUpdateSource { get; set; }
        public bool CccStock { get; private set; }
        public Currency Currency { get; private set; }
        public Exchange Exchange { get; private set; }
        //public List<StockDataUpdateSource>? StockDataSources { get; set; }
        private HashSet<StockDataUpdateSource> _stockDataUpdateSources;
        public IReadOnlyCollection<StockDataUpdateSource> StockDataUpdateSources => _stockDataUpdateSources?.ToList();
        public List<HistoricalDividend>? DividendHistory { get; set; }

        public double Yield {
            get { return Dividend / CurrentPrice * 100; }
        }
        public double Coverage {
            get { return Dividend / EpsTtm * 100; }
        }
        public double PE {
            get { return CurrentPrice / EpsTtm; }
        }

        private Stock() { } //For EF Core

        public Stock(string ticker, string name, double price, double dividend, int dgYears, double eps, Currency currency, Exchange exchange, DataUpdateSource creationSource, string? sector=null, DateTime? dataLastUpdated=null)
        {
            _stockDataUpdateSources = new HashSet<StockDataUpdateSource>();

            Ticker = ticker;
            Name = name;
            UpdatePrice(price, creationSource, null, dataLastUpdated);
            Sector = sector;
            UpdateDividend(dividend, creationSource, null, dataLastUpdated);
            UpdateDgYears(dgYears, creationSource, null, dataLastUpdated);
            UpdateEps(eps, creationSource, null, dataLastUpdated);
            Currency = currency;
            Exchange = exchange;
            CccStock = creationSource == DataUpdateSource.US_CCC;
        }

        public void UpdatePrice(double newPrice, DataUpdateSource source, DbContext? context, DateTime? lastUpdated = null) {
            CurrentPrice = newPrice;
            UpdateDataSource(StockAttribute.Price, source, context, lastUpdated);
        }

        public void UpdateDividend(double dividend, DataUpdateSource source, DbContext? context, DateTime? lastUpdated = null) { 
            Dividend = dividend;
            UpdateDataSource(StockAttribute.Dividend, source, context, lastUpdated);
        }
        public void UpdateDgYears(int dgYears, DataUpdateSource source, DbContext? context, DateTime? lastUpdated = null) { 
            DGYears = dgYears;
            UpdateDataSource(StockAttribute.DgYears, source, context, lastUpdated);
        }
        public void UpdateEps(double eps, DataUpdateSource source, DbContext? context, DateTime? lastUpdated = null) { 
            EpsTtm = eps;
            UpdateDataSource(StockAttribute.EpsTtm, source, context, lastUpdated);
        }
        public void UpdateEpsGrowth5(double epsG5, DataUpdateSource source, DbContext? context, DateTime? lastUpdated = null) { 
            EpsGrowth5 = epsG5;
            UpdateDataSource(StockAttribute.EpsGrowth5, source, context, lastUpdated);
        }
        public void UpdateDividendGrowth(double divGro1, double divGro3, double divGro5, double divGro10, DataUpdateSource source, DbContext? context = null, DateTime? lastUpdated = null) {
            DivGrowth1 = divGro1; UpdateDataSource(StockAttribute.DivGrowth1, source, context, lastUpdated);
            DivGrowth3 = divGro3; UpdateDataSource(StockAttribute.DivGrowth3, source, context, lastUpdated);
            DivGrowth5 = divGro5; UpdateDataSource(StockAttribute.DivGrowth5, source, context, lastUpdated);
            DivGrowth10 = divGro10; UpdateDataSource(StockAttribute.DivGrowth10, source, context, lastUpdated);
        }
        public void SetCccStatus(bool isOnCccList) { CccStock = isOnCccList; }
        
        private void UpdateDataSource(StockAttribute attribute, DataUpdateSource source, DbContext? context, DateTime? lastUpdated = null)
        {
            if (_stockDataUpdateSources == null)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context), "Context must be provided if StockDataUpdateSources hasnt been loaded");
                context.Entry(this).Collection(s => s.StockDataUpdateSources).Load();            
            }
            var dataUpdateSource = _stockDataUpdateSources.SingleOrDefault(u => u.Attribute == attribute);
            if (dataUpdateSource == null)
            {
                dataUpdateSource = new StockDataUpdateSource(attribute, source);
                _stockDataUpdateSources.Add(dataUpdateSource);
            }
            dataUpdateSource.UpdateLastUpdated(lastUpdated != null ? lastUpdated.Value : DateTime.UtcNow);
        }

    }
}
