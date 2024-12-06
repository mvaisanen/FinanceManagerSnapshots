using Common;
using Financemanager.Server.Database.Domain;
using FinanceManager.Server.Database;
using NLog;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Server.Services
{
    public class FxService: IDisposable
    {
        private readonly FinanceManagerContext _ctx;
        private Logger _log;
        public readonly int InstanceNumber;
        private static int InstancesCreated;

        public FxService(FinanceManagerContext ctx)
        {
            _log = LogManager.GetCurrentClassLogger();
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);
            _log.Debug($"FxService #{InstanceNumber} created with FinanceManagerContext instance #{ctx.InstanceNumber}");
            _ctx = ctx;
            //_currentRates = _ctx.CurrencyRates.OrderByDescending(c => c.Date).Take(20).Where(c => c.NoData == false).First(); //Testattava
        }

        public async Task<CurrencyRates> GetCurrentRates()
        {
            //await GetAndSaveFxRates(DateTime.Today.AddDays(-10), DateTime.Today, true);
            var latest = _ctx.CurrencyRates.OrderByDescending(c => c.Date).Take(20).Where(c => c.NoData == false).First(); //Testattava
            if (latest != null)
                return latest;
            //Fetch last 10 days if no new enough data in db
            await GetAndSaveFxRates(DateTime.Today.AddDays(-10), DateTime.Today, true);
            latest = _ctx.CurrencyRates.OrderByDescending(c => c.Date).Take(20).Where(c => c.NoData == false).First(); //Testattava
            if (latest != null)
                return latest;
            throw new InvalidOperationException("Unable to get current fx rates"); //Should never happen        
            
        }

        public async Task UpdateRecentRates()
        {
            _log.Debug("UpdateRecentRates...");
            await GetAndSaveFxRates(DateTime.UtcNow.AddDays(-10).Date, DateTime.UtcNow.Date);
            _log.Debug("UpdateRecentRates task done.");
        }

        public async Task FetchLongDurationHistoricalRatesIfNeeded()
        {
            if(_ctx.CurrencyRates.Count() == 0)
            {
                _log.Debug("No currency rates in database, fetching rates from 2009 onward...");
                await FetchRatesForTimerange(new DateTime(2009, 1, 1), DateTime.UtcNow);
            }
        }

        public async Task FetchRatesForTimerange(DateTime start, DateTime end)
        {
            _log.Debug($"FetchRatesForTimerange(), start={start.ToShortDateString()}, end={end.ToShortDateString()}");
            await GetAndSaveFxRates(start, end, true);
            _log.Debug("FetchRatesForTimerange() done.");
        }

        public async Task<double> GetFxRate(DateTime date, Currency homeCurrency, Currency foreignCurrency)
        {
            return await GetFxRate(date, homeCurrency, foreignCurrency, true);
        }

        //TODO: If we fetch rate for current day, ecb may (test!) return current rates which will change during day. 
        //This kind of data should not be saved to db or at least need to be later refetched
        private async Task<double> GetFxRate(DateTime date, Currency homeCurrency, Currency foreignCurrency, bool fetchIfNotFound = true)
        {
            _log.Debug($"Getting Fx rate against Euro for {foreignCurrency.ToString()} on {date.ToShortDateString()}, fetchIfNotFound={fetchIfNotFound}");
            if (homeCurrency == foreignCurrency) return 1.00;

            var match = _ctx.CurrencyRates.FirstOrDefault(c => c.Date.Date == date.Date);

            if (match != null && !match.NoData)
                return match.GetFxRate(homeCurrency, foreignCurrency);
            else if (date.Date == DateTime.UtcNow.Date || (match != null && match.NoData)) //Request for current day, or we have requested rate for this day before, but date is at weekend etc -> Try to use previous rate
            {
                _log.Debug("Request was for current day or found a date match with NoData - attempting to use closest earlier rate");
                var earlierDate = date.AddDays(-1);
                var attempt = 0;
                while (attempt < 10) //Try to look for a rate from closest earlier day that has data, max 10 days old
                {
                    match = _ctx.CurrencyRates.FirstOrDefault(c => c.Date.Date == earlierDate.Date /*&& !c.NoData*/);
                    if (match != null && !match.NoData)
                        return match.GetFxRate(homeCurrency, foreignCurrency);
                    else if (match == null) //We have never tried to fetch data for this day --> Do it now, also for some earlier days so we dont return here on next loop
                    {
                        _log.Debug($"Rates for {earlierDate.ToShortDateString()} have nothing on database, attempting to fetch, including earlier 10 days");
                        await GetAndSaveFxRates(date.AddDays(-10), date); //fetch data from ecb for this and previous 10 days, save to db
                        match = _ctx.CurrencyRates.FirstOrDefault(c => c.Date.Date == earlierDate.Date && !c.NoData); //try again, did we now get non-NoData rates for this day?
                        if (match != null)
                            return match.GetFxRate(homeCurrency, foreignCurrency);
                    }
                    earlierDate = earlierDate.AddDays(-1);
                    attempt++;
                }
                throw new ArgumentException($"Requested fx rate for {date.Date.ToShortDateString()} not found");
            }


            //Database didnt have any data (not even NoData) for this date (and it's not current day)
            if (fetchIfNotFound)
            {
                await GetAndSaveFxRates(date.AddDays(-10), date); //Fetch data for previous 10 days
                return await GetFxRate(date, homeCurrency, foreignCurrency, false); // try to return data after the fetch, dont do another fetch if no match
            }

            throw new ArgumentException($"Requested fx rate for {date.Date.ToShortDateString()} not found");
        }



        private async Task GetAndSaveFxRates(DateTime start, DateTime end, bool overwrite = true)
        {
            _log.Debug($"GetAndSaveFxRates() called, dates from {start.ToShortDateString()} to {end.ToShortDateString()}");
            /*
            ...
            <Series FREQ="D" CURRENCY="CAD" CURRENCY_DENOM="EUR" EXR_TYPE="SP00" EXR_SUFFIX="A" COLLECTION="A" UNIT_MULT="0" SOURCE_AGENCY="4F0" TITLE_COMPL="ECB reference exchange rate, Canadian dollar/Euro, 2:15 pm (C.E.T.)" UNIT="CAD" TITLE="Canadian dollar/Euro" DECIMALS="4">
                <Obs TIME_PERIOD="2018-04-03" OBS_VALUE="1.5818" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-04" OBS_VALUE="1.5756" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-05" OBS_VALUE="1.5659" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-06" OBS_VALUE="1.565" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-09" OBS_VALUE="1.5726" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-10" OBS_VALUE="1.5645" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-11" OBS_VALUE="1.5625" OBS_STATUS="A" OBS_CONF="F"/>
            </Series>
            <Series FREQ="D" CURRENCY="DKK" CURRENCY_DENOM="EUR" EXR_TYPE="SP00" EXR_SUFFIX="A" UNIT="DKK" COLLECTION="A" UNIT_MULT="0" SOURCE_AGENCY="4F0" TITLE_COMPL="ECB reference exchange rate, Danish krone/Euro, 2:15 pm (C.E.T.)" TITLE="Danish krone/Euro" DECIMALS="4">
                <Obs TIME_PERIOD="2018-04-03" OBS_VALUE="7.4492" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-04" OBS_VALUE="7.4499" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-05" OBS_VALUE="7.4472" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-06" OBS_VALUE="7.4474" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-09" OBS_VALUE="7.4469" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-10" OBS_VALUE="7.4468" OBS_STATUS="A" OBS_CONF="F"/>
                <Obs TIME_PERIOD="2018-04-11" OBS_VALUE="7.4449" OBS_STATUS="A" OBS_CONF="F"/>
            </Series>
            <Series.....
            ...
             */
            if (!overwrite)
            {
                var existingData = _ctx.CurrencyRates.Where(c => c.Date >= start && c.Date <= end);
                var hasMissingData = false;

                for (DateTime d = start; d <= end; d = d.AddDays(1))
                {
                    var dbData = existingData.FirstOrDefault(e => e.Date == d.Date);
                    if (dbData == null)
                    {
                        hasMissingData = true;
                        break;
                    }
                }

                if (!hasMissingData)
                {
                    _log.Debug($"GetAndSaveFxRates() skipping ECB fetch from {start.ToShortDateString()} to {end.ToShortDateString()} since no overwrite and db has all data");
                }
            }

            string startDay = start.Day <= 9 ? $"0{start.Day}" : start.Day.ToString();
            string startMonth = start.Month <= 9 ? $"0{start.Month}" : start.Month.ToString();
            string endDay = end.Day <= 9 ? $"0{end.Day}" : end.Day.ToString();
            string endMonth = end.Month <= 9 ? $"0{end.Month}" : end.Month.ToString();
            var url = $"https://sdw-wsrest.ecb.europa.eu/service/data/EXR/D.USD+CAD+DKK+NOK+SEK+GBP.EUR.SP00.A?startPeriod={start.Year}-{startMonth}-{startDay}&endPeriod={end.Year}-{endMonth}-{endDay}";

            _log.Debug($"Requesting data from ECB: " + url);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            CookieContainer cookieContainer = new CookieContainer();
            request.Method = "GET";
            request.KeepAlive = true;
            request.CookieContainer = cookieContainer;
            request.Accept = "application/vnd.sdmx.structurespecificdata+xml;version=2.1";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12;
            //| SecurityProtocolType.Ssl3;


            WebResponse response = await request.GetResponseAsync();
            Stream dataStream = response.GetResponseStream();

            List<CurrencyRates> received = new List<CurrencyRates>();

            using (var xmlReader = XmlReader.Create(dataStream))
            {
                string currency = "";
                while (xmlReader.Read())
                {
                    if (xmlReader.IsStartElement() && xmlReader.LocalName == "Series")
                    {
                        // we are inside the Group element. We can now read its attributes
                        currency = xmlReader.GetAttribute("CURRENCY");
                    }
                    else if (xmlReader.IsStartElement() && xmlReader.LocalName == "Obs")
                    {
                        string time = xmlReader.GetAttribute("TIME_PERIOD");
                        string valueStr = xmlReader.GetAttribute("OBS_VALUE");
                        double value = double.Parse(valueStr, CultureInfo.InvariantCulture);
                        if (double.IsNaN(value))
                            continue;

                        DateTime date;
                        if (DateTime.TryParse(time, out date))
                        {
                            //Do we already have entry waiting to be added with this date (but with this currency likely missing)?
                            var existing = received.FirstOrDefault(c => c.Date == date);

                            if (existing == null) // not already in received list (with another currency for this day)
                            {
                                var entry = new CurrencyRates() { Date = date.Date };
                                if (currency == "CAD") entry.EurCad = value;
                                else if (currency == "USD") entry.EurUsd = value;
                                else if (currency == "DKK") entry.EurDkk = value;
                                else if (currency == "NOK") entry.EurNok = value;
                                else if (currency == "SEK") entry.EurSek = value;
                                else if (currency == "GBP") entry.EurGbp = value;
                                received.Add(entry);
                            }
                            else
                            {
                                if (currency == "CAD") existing.EurCad = value;
                                else if (currency == "USD") existing.EurUsd = value;
                                else if (currency == "DKK") existing.EurDkk = value;
                                else if (currency == "NOK") existing.EurNok = value;
                                else if (currency == "SEK") existing.EurSek = value;
                                else if (currency == "GBP") existing.EurGbp = value;
                            }
                        }
                    }

                }
                //_ctx.CurrencyRates.AddRange(added);
            }

            //Find matching data from ecb reply compared to our request; If no entry in reply, mark that date as NoData to database so we know we have tried to fetch that
            for(DateTime d=start; d<=end; d = d.AddDays(1))
            {
                _log.Debug($"Checking parsed ECB reply list for fx rates for {d.Date.ToShortDateString()}");
                var receivedData = received.FirstOrDefault(r => r.Date.Date == d.Date);
                if (receivedData != null) //We got actual rate-date for this request dayte
                {
                    _log.Debug($"ECB reply has data for date {d.Date.ToShortDateString()}, checking database..");
                    var existing = _ctx.CurrencyRates.FirstOrDefault(c => c.Date == receivedData.Date);
                    if (existing == null)
                    {
                        _log.Debug($"No fx data in database for {d.Date.ToShortDateString()} - adding new");
                        _ctx.CurrencyRates.Add(receivedData);
                    }
                    else if (overwrite)
                    {
                        _log.Debug($"Database had fx rates for {receivedData.Date.ToShortDateString()}, overwriting...");
                        existing.EurCad = receivedData.EurCad;
                        existing.EurDkk = receivedData.EurDkk;
                        existing.EurGbp = receivedData.EurGbp;
                        existing.EurNok = receivedData.EurNok;
                        existing.EurSek = receivedData.EurSek;
                        existing.EurUsd = receivedData.EurUsd;
                        existing.NoData = false;
                    }
                    else
                    {
                        _log.Debug($"Received ECB rate-data for {receivedData.Date} but overwrite={overwrite} - skipping save");
                    }
                }
                else
                {
                    _log.Debug($"ECB reply doesn't have data for requested date {d.Date.ToShortDateString()} - checking db just in case");
                    var existing = _ctx.CurrencyRates.FirstOrDefault(c => c.Date == d.Date);
                    
                    if (existing == null)
                    {
                        _log.Debug($"Database doesnt have fx data for {d.Date.ToShortDateString()} either");
                        //Only add new NoData entry if no data for this day in db, and it's not current day (might not be updated yet on ecb end, usually around 16:00 CET)
                        if (d.Date != DateTime.UtcNow.Date)
                        {
                            _log.Debug($"Adding new NoData entry to fx database for {d.Date.ToShortDateString()}");
                            var emptyData = new CurrencyRates() { Date = d, NoData = true };
                            _ctx.CurrencyRates.Add(emptyData);
                        }
                    }
                    else
                    {
                        if (existing.NoData == false)
                            _log.Warn($"Got no data from ECB for day {d.ToShortDateString()} but database had non-NoData entry - overwriting");
                        existing.NoData = true;
                    }
                }
            }

            //Note: Might be good idea to have scheduled check for db integrity regarding entries where some rates are zero. Above code will add even
            //if only date and one rate gets filled
            _ctx.SaveChanges();
            _log.Debug($"GetAndSaveFxRates() from {start.ToShortDateString()} to {end.ToShortDateString()} done");
        }

        public async void Dispose()
        {
            _log.Debug($"Disposing FxService #{InstanceNumber}");
            await _ctx.DisposeAsync();
        }
    }


}
