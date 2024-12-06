using Akka.Actor;
using Akka.Event;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Financemanager.Server.Database.Domain;
using Server.Services;
using FinanceManager.Server.Database;

namespace Server.Actors
{
    public class SchedulerActor: ReceiveActor
    {
        private IActorRef _stockActor;
        //private IActorRef _fxActor;
        //private FxService _fxService;
        ILoggingAdapter _log;
        IServiceScopeFactory _serviceScopeFactory;
        //IServiceScope _scope;

        //public SchedulerActor(IServiceScopeFactory serviceScopeFactory, StockActorProvider stockProvider)
        public SchedulerActor(IServiceScopeFactory serviceScopeFactory, StockActorProvider stockProvider)
        {           
            _log = Context.GetLogger();
            _log.Debug("SchedulerActor startup...");
            //_scope = sp.CreateScope();
            _serviceScopeFactory = serviceScopeFactory;
            _stockActor = stockProvider.Invoke();
            //_fxActor = fxProvider.Invoke();

            Become(Ready);
        }

        private void Ready()
        {
            Receive<CheckForUpdateNeeds>(chk =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                //using (var ctx = _scope.ServiceProvider.GetRequiredService<FinanceManagerContext>())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var todaysUpdates = ctx.IexUpdateRuns.Where(s => s.TimeStamp.Date == DateTime.UtcNow.Date).OrderByDescending(o => o.TimeStamp).ToList();

                    IexUpdateRun upd = null;

                    if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                    {
                        //do nothing, at least for now
                    }
                    else if (DateTime.UtcNow.Hour > 20 || (DateTime.UtcNow.Hour == 20 && DateTime.UtcNow.Minute >= 20)) //TODO: USA times for NYSE etc. 23:20 EET
                    {
                        //No updates today, or some updates, but not after-close one
                        if (todaysUpdates == null || !todaysUpdates.Any(u => u.UpdateType == "AFTERCLOSE"))                  
                        {
                            upd = new IexUpdateRun() { TimeStamp = DateTime.UtcNow, UpdateType = "AFTERCLOSE" };                            
                        }
                    }
                    else if (DateTime.UtcNow.Hour > 19 || (DateTime.UtcNow.Hour == 19 && DateTime.UtcNow.Minute >= 10)) //22:10 EET
                    {
                        if (todaysUpdates == null || !todaysUpdates.Any(u => u.UpdateType == "LASTHOUR")) 
                        {
                            upd = new IexUpdateRun() { TimeStamp = DateTime.UtcNow, UpdateType = "LASTHOUR" };
                        }
                    }
                    else if (DateTime.UtcNow.Hour > 17 || (DateTime.UtcNow.Hour == 17 && DateTime.UtcNow.Minute >= 20)) //20:20 EET
                    {
                        if (todaysUpdates == null || !todaysUpdates.Any(u => u.UpdateType == "MID2")) 
                        {
                            upd = new IexUpdateRun() { TimeStamp = DateTime.UtcNow, UpdateType = "MID2" };
                        }
                    }
                    else if (DateTime.UtcNow.Hour > 15 || (DateTime.UtcNow.Hour == 15 && DateTime.UtcNow.Minute >= 30)) //18:30 EET
                    {
                        if (todaysUpdates == null || !todaysUpdates.Any(u => u.UpdateType == "MID1"))
                        {
                            upd = new IexUpdateRun() { TimeStamp = DateTime.UtcNow, UpdateType = "MID1" };
                        }
                    }
                    else if (DateTime.UtcNow.Hour > 13 || (DateTime.UtcNow.Hour == 13 && DateTime.UtcNow.Minute >= 40)) //16:40 EET
                    {
                        if (todaysUpdates == null || !todaysUpdates.Any(u => u.UpdateType == "AFTEROPEN"))
                        {
                            upd = new IexUpdateRun() { TimeStamp = DateTime.UtcNow, UpdateType = "AFTEROPEN" };
                        }
                    }

                    if (upd != null)
                    {
                        _log.Debug($"Scheduler has determined the need to do a {upd.UpdateType} IEX update, adding to db and messaging StockActor...");
                        ctx.IexUpdateRuns.Add(upd);
                        ctx.SaveChanges();
                        if (upd.UpdateType == "AFTERCLOSE")
                            _stockActor.Tell(StockMessage.NewUpdateAllIexStockPrices(upd.Id));
                        else
                            _stockActor.Tell(StockMessage.NewUpdateRelevantIexStockPrices(upd.Id));
                    }
                    else
                    {
                        _log.Debug($"Scheduler has determined there is no need for IEX update");
                    }
                }

                var self = Self;
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(120), self, new CheckForUpdateNeeds(), ActorRefs.NoSender);
            });

            Receive<SchedulerMessage.UpdateRelevantStockPrices>(msg =>
            {
                _log.Debug("Telling stockActor to update relevant prices...");
                //TODO: Dont try to update when market isnt open
                _stockActor.Tell(StockMessage.NewUpdateRelevantStockPrices(true));
            });

            Receive<StockMessage.UpdateRelevantStockPricesDone>(res =>
            {
                _log.Debug("SchedulerActor received result from UpdateRelevantStockPrices, task succeedded = " + res.succeeded);
                //Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(60), Self, SchedulerMessage.NewUpdateRelevantStockPrices(true), Self);
            });


            Receive<SchedulerMessage.DoCurrencyUpdates>(msg =>
            {
                _log.Debug("Telling FxService to do currency rate updates...");
                var scope = _serviceScopeFactory.CreateScope();
                //using (var scope = _serviceScopeFactory.CreateScope())
                //{
                var _fxService = scope.ServiceProvider.GetRequiredService<FxService>();
                _fxService.UpdateRecentRates().ContinueWith(ca =>
                {
                    _log.Debug("_fxService.UpdateRecentRates() Task returned - disposing serviceScope");
                    scope.Dispose();
                });
                //}
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(60), Self, SchedulerMessage.NewDoCurrencyUpdates(5), ActorRefs.NoSender);
                _log.Debug("DoCurrencyUpdates() message handling finished");
                //_fxActor.Tell(CurrencyMessage.NewUpdateOfficialRates(DateTime.UtcNow.AddDays(-msg.backInDays), DateTime.UtcNow));

                //TODO: Some sort of timeout checking, if for some reason no reply received within x seconds, assume fail (if needed)
            });

            Receive<DoInitialDatabaseFxSeeding>(msg =>
            {
                _log.Debug("Telling FxService to fetch long-time initial fx data if needed");
                //using (var scope = _serviceScopeFactory.CreateScope())
                var scope = _serviceScopeFactory.CreateScope();
                
                var _fxService = scope.ServiceProvider.GetRequiredService<FxService>();
                _fxService.FetchLongDurationHistoricalRatesIfNeeded().ContinueWith(c =>
                {
                    _log.Debug("FetchLongDurationHistoricalRatesIfNeeded Task returned - disposing serviceScope");
                    scope.Dispose();
                });
                
                _log.Debug("DoInitialDatabaseFxSeeding message handling done");
            });

            Receive<DoAlphaVantageUpdatesTEST>(msg =>
            {
                _log.Debug("Received msg DoAlphaVantageUpdatesTEST");
                //var scope = _serviceScopeFactory.CreateScope();
                //
                //var _stockService = scope.ServiceProvider.GetRequiredService<StockService>();
                //_stockService.UpdateAlphavantagePrices().ContinueWith(c =>
                //{
                //    _log.Debug("UpdateAlphavantagePrices Task finished - disposing serviceScope");
                //    scope.Dispose();
                //});
            });

            Receive<DoFMPUpdatesTEST>(msg =>
            {
                _log.Debug("Received msg DoFMPUpdatesTEST");
                var scope = _serviceScopeFactory.CreateScope();

                var _stockService = scope.ServiceProvider.GetRequiredService<StockService>();
                /*_stockService.UpdateFMPPrices().ContinueWith(c =>
                {
                    _log.Debug("UpdateFMPPrices Task finished - disposing serviceScope");
                    scope.Dispose();
                });*/
                _stockService.UpdateDividendHistory("LEG").ContinueWith(c =>
                {
                    _log.Debug("UpdateDividendHistory Task finished - disposing serviceScope");
                    scope.Dispose();
                });

                var scope2 = _serviceScopeFactory.CreateScope();
                var _stockService2 = scope2.ServiceProvider.GetRequiredService<StockService>();
                _stockService2.UpdatePriceHistory("LEG").ContinueWith(c =>
                {
                    _log.Debug("UpdatePriceHistory Task finished - disposing serviceScope");
                    scope2.Dispose();
                });
            });

            /*Receive<DoRapidApiStockDataYFAltTEST>(test =>
            {
                _log.Debug("Received msg DoRapidApiStockDataYFAltTEST");
                var scope = _serviceScopeFactory.CreateScope();

                var _stockService = scope.ServiceProvider.GetRequiredService<StockService>();
                _stockService.DoRapidApiStockDataYFAlternativeDataUpdates().ContinueWith(c =>
                {
                    _log.Debug("DoRapidApiStockDataYFAlternativeUpdates Task finished - disposing serviceScope");
                    scope.Dispose();
                });
            });*/

            //all various data (incl. price) updates for stocks. Todo: all kinds of updates here
            Receive<DoStockDataUpdatesIfNeeded>(upd =>
            {
                _log.Debug("Received msg DoStockDataUpdates");
                var scope = _serviceScopeFactory.CreateScope();

                string northAmericaPriceUpdateType = null;
                string usPriceUpdateType = null;
                string euroPriceUpdateType = null; //on weekdays
                string weekendDataUpdateType = null;

                using (var ctx = scope.ServiceProvider.GetRequiredService<FinanceManagerContext>())
                {
                    /*var lastRapidApiStockDataYFAltUpdate = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "SATURDAY_RASDYFA").OrderByDescending(o => o.TimestampUtc).FirstOrDefault();
                    var lastUpd = lastRapidApiStockDataYFAltUpdate?.TimestampUtc;
                    var now = DateTime.UtcNow;
                    //update if update never done, last done over a week ago, or last done over 5 days ago and now is saturday (ie last update on prev. week)
                    if (lastRapidApiStockDataYFAltUpdate == null || now - lastUpd > TimeSpan.FromDays(7) || (now - lastUpd >= TimeSpan.FromDays(5) && now.DayOfWeek == DayOfWeek.Saturday))
                        doRapidApiStockDataYFAlternativeSaturdayUpds = true;*/
                    DateTime? lastUpd = null;
                    var now = DateTime.UtcNow;
                    //Price updates for european stocks: Every weekday after 11 and 17 Eastern European time (EET)
                    var eetZone = TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time");
                    var eetNow = TimeZoneInfo.ConvertTimeFromUtc(now, eetZone);
                    var etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    var etNow = TimeZoneInfo.ConvertTimeFromUtc(now, etZone);

                    if (!IsWeekend(now))
                    {
                        //North America exchanges: 9:30 am - 4 pm
                        if (etNow.Hour >= 17 || (etNow.Hour == 16) && etNow.Minute >= 20) //Not earlier than 16:20, market closes at 16
                        {
                            var lastUpdRun = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_NA_AFTERCLOSE").OrderByDescending(o => o.TimestampUtc).FirstOrDefault();
                            lastUpd = lastUpdRun?.TimestampUtc;
                            if (lastUpd == null || lastUpd.Value.Date != now.Date)
                                northAmericaPriceUpdateType = "WEEKDAY_NA_AFTERCLOSE";

                            var lastUsUpd = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_US_AFTERCLOSE").OrderByDescending(o => o.TimestampUtc).FirstOrDefault()?.TimestampUtc;
                            if (lastUsUpd == null || lastUsUpd.Value.Date != now.Date)
                                usPriceUpdateType = "WEEKDAY_US_AFTERCLOSE";
                        }
                        else if (etNow.Hour >= 14) //Usually 21:00 eet
                        {
                            var lastUsUpd = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_US_MID2").OrderByDescending(o => o.TimestampUtc).FirstOrDefault()?.TimestampUtc;
                            if (lastUsUpd == null || lastUsUpd.Value.Date != now.Date)
                                usPriceUpdateType = "WEEKDAY_US_MID2";
                        }
                        else if (etNow.Hour >= 13) //Usually 20:00 eet
                        {
                            var lastUpdRun = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_NA_MID").OrderByDescending(o => o.TimestampUtc).FirstOrDefault();
                            lastUpd = lastUpdRun?.TimestampUtc;
                            if (lastUpd == null || lastUpd.Value.Date != now.Date)
                                northAmericaPriceUpdateType = "WEEKDAY_NA_MID";
                        }
                        else if (etNow.Hour >= 12) //Usually 19:00 eet
                        {
                            var lastUsUpd = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_US_MID1").OrderByDescending(o => o.TimestampUtc).FirstOrDefault()?.TimestampUtc;
                            if (lastUsUpd == null || lastUsUpd.Value.Date != now.Date)
                                usPriceUpdateType = "WEEKDAY_US_MID1";
                        }
                        else if (etNow.Hour >= 10) //Usually 17:00 eet
                        {
                            var lastUpdRun = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_NA_AFTEROPEN").OrderByDescending(o => o.TimestampUtc).FirstOrDefault();
                            lastUpd = lastUpdRun?.TimestampUtc;
                            if (lastUpd == null || lastUpd.Value.Date != now.Date)
                                northAmericaPriceUpdateType = "WEEKDAY_NA_AFTEROPEN";

                            var lastUsUpd = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_US_AFTEROPEN").OrderByDescending(o => o.TimestampUtc).FirstOrDefault()?.TimestampUtc;
                            if (lastUsUpd == null || lastUsUpd.Value.Date != now.Date)
                                usPriceUpdateType = "WEEKDAY_US_AFTEROPEN";
                        }

                        if (eetNow.Hour >= 17)
                        {
                            var lastUpdRun = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_EURO_17").OrderByDescending(o => o.TimestampUtc).FirstOrDefault();
                            lastUpd = lastUpdRun?.TimestampUtc;
                            if (lastUpd == null || lastUpd.Value.Date != now.Date)
                                euroPriceUpdateType = "WEEKDAY_EURO_17";
                        }
                        else if (now.Hour >= 11)
                        {
                            var lastUpdRun = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKDAY_EURO_11").OrderByDescending(o => o.TimestampUtc).FirstOrDefault();
                            lastUpd = lastUpdRun?.TimestampUtc;
                            if (lastUpd == null || lastUpd.Value.Date != now.Date)
                                euroPriceUpdateType = "WEEKDAY_EURO_11";
                        }
                    }
                    else if (now.Hour > 4) //Weekend and after 4 am utc
                    {
                        //TODO: Some weekend updates. Dividend history, price history etc
                        var lastUpdRun = ctx.ApiUpdateRuns.Where(r => r.UpdateType == "WEEKEND_DATAS").OrderByDescending(o => o.TimestampUtc).FirstOrDefault();
                        lastUpd = lastUpdRun?.TimestampUtc;
                        if (lastUpd == null || now - lastUpd.Value >= TimeSpan.FromDays(3) )
                            weekendDataUpdateType = "WEEKEND_DATAS"; //YahooFinanceStockDataYFAlternative data updates for non-FMP stocks: eps, dividend, etc basic info
                        //TODO: May no immediate need for this, since YHFinance quotes have started to return dividend and eps now.
                    }
                    
                }
                scope.Dispose();

                var update = false;
                if (!string.IsNullOrEmpty(euroPriceUpdateType))
                {
                    _log.Debug($"Scheduler has determined a need for EuroPriceUpdate {euroPriceUpdateType}");
                    update = true;
                    var euroScope = _serviceScopeFactory.CreateScope();
                    var _stockService = euroScope.ServiceProvider.GetRequiredService<StockService>();
                    _stockService.DoEuropeanPriceUpdates(euroPriceUpdateType).ContinueWith(c =>
                    {
                        if (!c.IsFaulted)
                        {
                            var updates = c.Result;
                            _log.Debug($"DoEuropeanPriceUpdates Task finished, updated {updates} prices - disposing serviceScope");
                        }
                        else
                            _log.Debug($"DoEuropeanPriceUpdates Task finished with exception: {c.Exception} - disposing serviceScope");
                        euroScope.Dispose();
                    });
                }

                if (!string.IsNullOrEmpty(northAmericaPriceUpdateType))
                {
                    _log.Debug($"Scheduler has determined a need for NorthAmericaPriceUpdate {northAmericaPriceUpdateType}");
                    update = true;
                    var naScope = _serviceScopeFactory.CreateScope();
                    var _stockService = naScope.ServiceProvider.GetRequiredService<StockService>();
                    _stockService.DoNorthAmericaPriceUpdates(northAmericaPriceUpdateType).ContinueWith(c =>
                    {
                        if (!c.IsFaulted)
                        {
                            var updates = c.Result;
                            _log.Debug($"DoNorthAmericaPriceUpdates Task finished, updated {updates} prices - disposing serviceScope");
                        }
                        else
                            _log.Debug($"DoNorthAmericaPriceUpdates Task finished with errors - disposing serviceScope");
                        naScope.Dispose();
                    });
                }

                if (!string.IsNullOrEmpty(usPriceUpdateType))
                {
                    _log.Debug($"Scheduler has determined a need for UsPriceUpdate {usPriceUpdateType}");
                    update = true;
                    var naScope = _serviceScopeFactory.CreateScope();
                    var _stockService = naScope.ServiceProvider.GetRequiredService<StockService>();
                    _stockService.DoFmpPriceUpdates(usPriceUpdateType).ContinueWith(c =>
                    {
                        if (!c.IsFaulted)
                        {
                            var updates = c.Result;
                            _log.Debug($"DoFmpPriceUpdates Task finished, updated {updates} prices - disposing serviceScope");
                        }
                        else
                            _log.Debug($"DoFmpPriceUpdates Task finished with errors - disposing serviceScope");
                        naScope.Dispose();
                    });
                }

                if (!update)
                    _log.Debug("Scheduler has determined there is no need for any stock updates");

                //TODO: Set a flag somewhere which updates are running, so if scheduler gets to this msg again before they have finished (and written timestamps)
                //they dont get accidentally started again!

                var self = Self;
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(120), self, new DoStockDataUpdatesIfNeeded(), ActorRefs.NoSender);
            });

            var self = Self;
            //Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(30), _stockActor, StockMessage.NewUpdateRelevantStockPrices(true), Self);

            //Pois 5.2.2022 kun testataan auth muutoksia niin ei spämmää päivityksiä
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(10), self, SchedulerMessage.NewDoCurrencyUpdates(5), ActorRefs.NoSender);
            //Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(30), self, new CheckForUpdateNeeds(), ActorRefs.NoSender); //This was for Iex only, no longer used (free api ended)
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(5), self, new DoInitialDatabaseFxSeeding(), ActorRefs.NoSender);
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(20), self, new DoStockDataUpdatesIfNeeded(), ActorRefs.NoSender);
            //Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(20), self, new DoFMPUpdatesTEST(), ActorRefs.NoSender);
        }

        private bool IsWeekend(DateTime utcNow)
        {
            if (utcNow.DayOfWeek == DayOfWeek.Saturday || utcNow.DayOfWeek == DayOfWeek.Sunday)
                return true;
            return false;
        }

        protected override void PostStop()
        {
            base.PostStop();
        }
    }

    public class CheckForUpdateNeeds { }

    public class DoInitialDatabaseFxSeeding { }

    public class DoAlphaVantageUpdatesTEST { }
    public class DoFMPUpdatesTEST { }
    public class DoRapidApiStockDataYFAltTEST { }
    public class DoStockDataUpdatesIfNeeded { }
}
