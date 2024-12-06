using Microsoft.EntityFrameworkCore;
using NLog;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceManager.Server.Database
{
    public class FinanceManagerContext: DbContext
    {
        ILogger _log = LogManager.GetCurrentClassLogger();
        public readonly int InstanceNumber;
        private static int InstancesCreated;

        public FinanceManagerContext(DbContextOptions<FinanceManagerContext> options) : base(options)
        {
            // three minute command timeout
            //this.Database.CommandTimeout = 180;
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);

            _log.Debug($"FinanceManagerContext #{InstanceNumber} created");
            //this.Configuration.LazyLoadingEnabled = false;
            //System.Data.Entity.Database.SetInitializer(new DropCreateDatabaseAlways<FinanceManagerContext>());
            //Database.SetInitializer(new FinanceManagerInitializer());
            //Database.SetInitializer<FinanceManagerContext>(new CreateDatabaseIfNotExists<FinanceManagerContext>());
        }

        public override void Dispose()
        {
            base.Dispose();
            _log.Debug($"FinanceManagerContext #{InstanceNumber} disposed");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockPurchase>()
                .HasOne<PortfolioPosition>()
                .WithMany(pp => pp.Buys)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }


        public DbSet<ReceivedDividend> Dividends { get; set; }
        public DbSet<CurrencyRates> CurrencyRates { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        //public DbSet<StockDataUpdateSource> StockDataUpdateSources { get; set; }
        //public DbSet<WatchlistStock> WatchlistStocks { get; set; }
        public DbSet<Watchlist> Watchlists { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        //public DbSet<PortfolioPosition> PortfolioPositions { get; set; }
        //public DbSet<StockPurchase> StockPurchases { get; set; }
        public DbSet<IexUpdateRun> IexUpdateRuns { get; set; }
        public DbSet<ApiUpdateRun> ApiUpdateRuns { get; set; }
        public DbSet<ApiProviderRun> ApiProviderRuns { get; set; }
        public DbSet<DividendsPlan> DividendPlans { get; set; }
        public DbSet<SSDScore> SSDScore { get; set; }
        public DbSet<HistoricalDividend> HistoricalDividend { get; set; }
        public DbSet<HistoricalPrice> HistoricalPrice { get; set; }
    }
}
