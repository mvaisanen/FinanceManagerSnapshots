using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Common;
using Common.Dtos;
using Common.HelperModels;
using NLog;
using Server.Database;
using Server.Mappings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Util;
using FinanceManager.Server.Database;
using Financemanager.Server.Database.Domain;

namespace Server.Services
{
    public class DividendService : IDisposable
    {
        private readonly FinanceManagerContext _ctx;
        private readonly FxService _fxService;
        private Logger _log;
        public readonly int InstanceNumber;
        private static int InstancesCreated;

        public DividendService(FinanceManagerContext ctx, FxService fxService)
        {
            _log = LogManager.GetCurrentClassLogger();
            InstanceNumber = Interlocked.Increment(ref InstancesCreated);
            _log.Debug($"DividendService #{InstanceNumber} created with FinanceManagerContext instance #{ctx.InstanceNumber}");
            _ctx = ctx;
            _fxService = fxService;
        }
       

        public async Task<DividendsChangedDto> UploadUsersIbDividends(string userId, bool multiAccount, FileUpload uploadedFile)
        {
            var dividends = _ctx.Dividends
                .Where(d => d.UserId == userId);
            int offset = multiAccount == true ? 1 : 0;
            var divsToAdd = new List<ReceivedDividend>();

            //var memStream = new MemoryStream(uploadedFile.Data);
            using (var memStream = new MemoryStream(uploadedFile.Data))            
            using (var file = new StreamReader(memStream))
            {
                var line = file.ReadLine();
                while (line != null)
                {
                    StringReader sr = new StringReader(line);
                    var parser = new TextFieldParser(sr);
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    string[] fields = { };
                    try
                    {
                        Console.WriteLine("trying to parse:" + line);
                        fields = parser.ReadFields();
                    }
                    catch (MalformedLineException mle)
                    {
                        _log.Warn($"Cannot parse line: {line}");
                        throw;
                    }

                    //Skip non-data and total rows
                    if (string.IsNullOrEmpty(fields[0]) || fields[0] != "Dividends" || fields[1] == "Header" || string.IsNullOrEmpty(fields[2]) || fields[2].Contains("Total"))
                    {
                        _log.Info($"UploadUsersIbDividends() skipping row {line}");
                        line = file.ReadLine();
                        continue;
                    }

                    //Dividends,Data,USD,2020-01-07,PEP(US7134481081) Cash Dividend USD 0.955 per Share (Ordinary Dividend),12.42,
                    var currencyOk = Enum.TryParse(typeof(Currency), fields[2 + offset], out var currency);
                    if (!currencyOk)
                        throw new ArgumentException($"Invalid currency: {fields[2 + offset]}");

                    var dateStr = fields[3 + offset];
                    var date = DateTime.Parse(dateStr);

                    var tickerAndInfo = fields[4 + offset];
                    var ticker = tickerAndInfo.Substring(0, tickerAndInfo.IndexOf('('));

                    var totalPayment = double.Parse(fields[5 + offset], CultureInfo.InvariantCulture);

                    var div = new ReceivedDividend(userId, date, ticker, ticker, null, null, totalPayment, (Currency)currency, Broker.InteractiveBrokers, null);

                    if (tickerAndInfo.Contains("Reversal"))
                    {
                        var match = divsToAdd.FirstOrDefault(d => d.CompanyTicker == ticker && d.PaymentDate == date && d.Currency == (Currency)currency);
                        if (match != null)
                        {
                            _log.Info($"Removing reverted dividend {ticker} {date} {totalPayment} {(Currency)currency}");
                            divsToAdd.Remove(match);
                        }
                        else
                        {
                            match = dividends.FirstOrDefault(d => d.CompanyTicker == ticker && d.PaymentDate == date && d.Currency == (Currency)currency);
                            if (match != null)
                            {
                                _log.Info($"Removing reverted dividend {ticker} {date} {totalPayment} {(Currency)currency}");
                                divsToAdd.Remove(match);
                            }
                            else
                                throw new ArgumentException("Reverted dividend that cannot be found!");
                        }
                    }
                    else
                    {
                        var existing = dividends.FirstOrDefault(d => d.Broker == Broker.InteractiveBrokers && d.CompanyTicker == ticker 
                            && d.PaymentDate == date && d.Currency == (Currency)currency);
                        if (existing == null)
                            divsToAdd.Add(div);
                        else
                            _log.Debug($"Dividend already exists, not adding: {ticker} {date.ToShortDateString()} {totalPayment} {currency}");
                    }

                    line = file.ReadLine();
                }

                foreach (var div in divsToAdd)
                {
                    _ctx.Dividends.Add(div);
                }
            }
            
            var res = await _ctx.SaveChangesAsync();
            _log.Debug($"IB dividends uploaded, total of {res} changes done.");
            var addedOrModified = new List<ReceivedDividendDTO>();
            foreach (var d in divsToAdd)
            {
                if (d.FxRate != null) //We dont have these here, but to make sure
                    addedOrModified.Add(d.ToDTO());
                else
                {
                    var rate = await _fxService.GetFxRate(d.PaymentDate, Currency.EUR, d.Currency); //TODO: Dont hardcode EUR to maybe someday support other home currencies
                    d.FxRate = rate;
                    addedOrModified.Add(d.ToDTO());
                }
            }
            return new DividendsChangedDto() { DividendsAddedOrModified = addedOrModified.ToList() };
        }




        public async Task<DividendsChangedDto> UploadUsersNordnetDividends(string userId,  FileUpload uploadedFile)
        {
            var dividends = _ctx.Dividends.Where(d => d.UserId == userId);
            var divsToAdd = new List<ReceivedDividend>();

            using (var memStream = new MemoryStream(uploadedFile.Data))
            using (var file = new StreamReader(memStream))
            {
                var line = file.ReadLine();
                while (line != null)
                {
                    StringReader sr = new StringReader(line);
                    var parser = new TextFieldParser(sr);
                    parser.SetDelimiters("\t");
                    parser.HasFieldsEnclosedInQuotes = true;

                    string[] fields = { };
                    try
                    {
                        Console.WriteLine("trying to parse:" + line);
                        fields = parser.ReadFields();
                    }
                    catch (MalformedLineException mle)
                    {
                        _log.Warn($"Cannot parse line: {line}");
                        throw;
                    }

                    //Skip non-dividend rows
                    if (fields.Length < 24 ||  fields[5] != "OSINKO" || fields[24] != "") //field 5 is type, field 24 is invalidated-date (div has been invalidated later)
                    {
                        _log.Info($"UploadUsersNordnetDividends() skipping row {line}");
                        line = file.ReadLine();
                        continue;
                    }

                    var dateStr = fields[3]; //Maksupäivä, ie Pay date
                    var ticker = fields[6];
                    var sharesStr = fields[9];
                    var divPerShareStr = fields[10].Replace(',', '.');
                    var fxRateStr = fields[22].Replace(',', '.');
                    var currencyStr = fields[15];
                    var totalStr = fields[14].Trim().Replace(',', '.');

                    //Id	Kirjauspäivä	Kauppapäivä	Maksupäivä	Salkku	Tapahtumatyyppi	Arvopaperi	Instrumenttityyppi	ISIN	Määrä	Kurssi	Korko	Kokonaiskulut	Kokonaiskulut Valuutta	Summa	Valuutta	Hankinta-arvo	Tulos	Kokonaismäärä	Saldo	Vaihtokurssi	Tapahtumateksti	Mitätöintipäivä	Laskelma	Vahvistusnumero	Välityspalkkio	Välityspalkkio Valuutta
                    //99887766543	28.12.2021	30.11.2021	27.12.2021	1234567	OSINKO	LMT	Aktier	US5398301094	9	2,8	0	0	USD	25,2	USD	0	0	0	1 412,89	0,884	OSINKO LMT 2.8 USD/OSAKE			932931833		
                    var currencyOk = Enum.TryParse(typeof(Currency), currencyStr, out var currency);
                    if (!currencyOk)
                        throw new ArgumentException($"Invalid currency: {currencyStr} on line '{line}'");

                    var date = DateTime.Parse(dateStr);
                    var totalPayment = double.Parse(totalStr, CultureInfo.InvariantCulture);
                    var shares = int.Parse(sharesStr);
                    var divPerShare = double.Parse(divPerShareStr, CultureInfo.InvariantCulture);
                    var divFxRate = double.Parse(fxRateStr, CultureInfo.InvariantCulture);

                    var div = new ReceivedDividend(userId, date, ticker, ticker, shares, divPerShare, totalPayment, (Currency)currency, Broker.Nordnet, 1.0/divFxRate);

                    var existing = dividends.FirstOrDefault(d => d.Broker == Broker.Nordnet && d.CompanyTicker == ticker
                        && d.PaymentDate == date && d.Currency == (Currency)currency);
                    if (existing == null)
                        divsToAdd.Add(div);
                    else
                        _log.Debug($"Dividend already exists, not adding: {ticker} {date.ToShortDateString()} {totalPayment} {currency}");
                    
                    line = file.ReadLine();
                }

                foreach (var div in divsToAdd)
                {
                    _log.Debug($"Adding Nordnet dividend to user {userId}: {div.CompanyTicker} {div.PaymentDate.ToShortDateString()} {div.TotalReceived} {div.Currency}");
                    _ctx.Dividends.Add(div);
                }
            }

            var res = await _ctx.SaveChangesAsync();
            _log.Debug($"Nordnet dividends uploaded, total of {res} changes done.");
            var addedOrModified = new List<ReceivedDividendDTO>();
            foreach (var d in divsToAdd)
            {
                if (d.FxRate != null) //Nordnet divs should always have this
                    addedOrModified.Add(d.ToDTO());
                else
                {
                    var rate = await _fxService.GetFxRate(d.PaymentDate, Currency.EUR, d.Currency); //TODO: Dont hardcode EUR to maybe someday support other home currencies
                    d.FxRate = rate;
                    addedOrModified.Add(d.ToDTO());
                }
            }
            return new DividendsChangedDto() { DividendsAddedOrModified = addedOrModified.ToList() };
           
        }



        public async Task<DividendsPlanDto> SaveDividendPlan(DividendsPlanDto dto, string userId)
        {
            var plan = _ctx.DividendPlans.FirstOrDefault(p => p.UserId == userId);
            if (plan == null)
            {
                plan = new DividendsPlan();
                _ctx.DividendPlans.Add(plan);
            }
            
            plan.UserId = userId;
            plan.StartDividends = dto.StartDividends;
            plan.NewDivGrowth = dto.NewDivGrowth;
            plan.CurrentDivGrowth = dto.CurrentDivGrowth;
            plan.StartYear = dto.StartYear;
            plan.YearlyInvestment = dto.YearlyInvestment;
            plan.Years = dto.Years;
            plan.Yield = dto.Yield;

            await _ctx.SaveChangesAsync();
            var planDto = plan.ToDTO();
            planDto.AlternativeYieldsAndGrowthsBasedOnPlan = CalculateAlternativeYieldsAndGrowthsFromPlan(planDto);
            return planDto;
        }

        public async Task<DividendsPlanDto> GetUsersDividendPlan(string userId)
        {
            var plan = _ctx.DividendPlans.FirstOrDefault(p => p.UserId == userId);
            if (plan != null)
            { 
                var planDto = plan.ToDTO();
                planDto.AlternativeYieldsAndGrowthsBasedOnPlan = CalculateAlternativeYieldsAndGrowthsFromPlan(planDto);
                return planDto;
            }
            else
                throw new ArgumentException("No dividend plans found for user");
        }

        private Dictionary<string, double> CalculateAlternativeYieldsAndGrowthsFromPlan(DividendsPlanDto plan)
        {
            var yearsRemaining = plan.Years - (DateTime.UtcNow.Year - plan.StartYear);
            var yearsFromStart = DateTime.UtcNow.Year - plan.StartYear;

            /*var planDivsNow = DivMath.CalculateEndDividends(plan.StartDividends, plan.CurrentDivGrowth, plan.YearlyInvestment, plan.Yield, plan.NewDivGrowth, yearsFromStart);
            return CalculateAlternativeYieldsAndGrowths(plan.Yield, plan.CurrentDivGrowth, plan.NewDivGrowth, planDivsNow, yearsRemaining, plan.YearlyInvestment)
                    .OrderBy(a => a.Key).ToDictionary(o => o.Key, o => o.Value);*/
            //var planDivsNow = DivMath.CalculateEndDividends(100, plan.Yield, plan.NewDivGrowth, yearsFromStart);
            return CalculateAlternativeYieldsAndGrowths(plan.Yield, plan.NewDivGrowth, yearsRemaining)
                    .OrderBy(a => a.Key).ToDictionary(o => o.Key, o => o.Value);
        }


        //Calculates different yield-growth pairs that would result in same final dividend goal as base case given current dividends and time remaining
        private Dictionary<string,double> CalculateAlternativeYieldsAndGrowths(double newYield,  double newGrowth, int yearsToGoal)
        {
            var reference = DivMath.CalculateEndDividends(100, newYield, newGrowth, yearsToGoal);
            var result = new Dictionary<string, double>() { { newYield.ToString(), newGrowth } };

            for (double yield = 2.0; yield <= 7.0; yield = yield + 0.25)
            {
                if (Math.Abs(yield - newYield) < 0.00001) //BaseYield/growth already in dictionary
                    continue;
                var growth = 0.0;
                while(DivMath.CalculateEndDividends(100, yield, growth, yearsToGoal) < reference || growth > 99) //growth>99 safeguard
                {
                    growth += 0.1;
                }
                result.Add(yield.ToString(), growth);
                if (growth == 0.0) //If we are up to a yield that needs 0 growth, no need to go higher
                    break;
            }

            return result;
        }

        



        public async void Dispose()
        {
            await _ctx.DisposeAsync();
        }
    }
}
