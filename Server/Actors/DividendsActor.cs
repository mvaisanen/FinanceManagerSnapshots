using Akka.Actor;
using Akka.Event;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Collections;
using Common;
using Common.Dtos;
using Server.Database;
using Server.Mappings;
using Server.Models;
//using Server.Models.Db;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using FinanceManager.Server.Database;
using Financemanager.Server.Database.Domain;

namespace Server.Actors
{
    public class DividendsActor: ReceiveActor
    {
        IServiceScopeFactory _serviceScopeFactory;
        ILoggingAdapter _log;
        //private IActorRef _fxRef;
        //private FxService _fxSrv;

        public DividendsActor(IServiceScopeFactory serviceScopeFactory)
        {                    
            _log = Context.GetLogger();
            _log.Debug("DividendActor startup...");
            _serviceScopeFactory = serviceScopeFactory;
            //_fxRef = fxProvider.Invoke();

            Become(Ready);
        }

        private void Ready()
        {
            ReceiveAsync<DividendMessage.GetDividendsByUserId>(async msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var _fxSrv = scope.ServiceProvider.GetRequiredService<FxService>();
                    var divs = ctx.Dividends.Where(d => d.UserId == msg.UserId).ToList();

                    var divsWithNoFx = divs.Where(d => d.FxRate == null);
                    //var missingFxDates = divsWithNoFx.Select(d => d.PaymentDate).ToList();
                    //var rates = _fxRef.Ask<DbOperationResult<Dictionary<DateTime, double>>>(CurrencyMessage.NewGetRatesForDates(Currency.EUR, Currency.USD, ListModule.OfSeq(missingFxDates))).Result;

                    foreach (var d in divsWithNoFx)
                    {
                        //var rate = rates.Item[d.PaymentDate];
                        var rate = await _fxSrv.GetFxRate(d.PaymentDate, Currency.EUR, d.Currency); //TODO: Dont hardcode EUR to maybe someday support other home currencies
                        d.FxRate = rate;
                    }

                    var divsDto = divs.Select(d => d.ToDTO()).OrderBy(d => d.PaymentDate).ToList();
                    var result = new DbOperationResult<List<ReceivedDividendDTO>>(divsDto);
                    Sender.Tell(result);
                }
            });

            ReceiveAsync<DividendMessage.GetTtmDividends>(async msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var _fxSrv = scope.ServiceProvider.GetRequiredService<FxService>();

                    var divs = ctx.Dividends.Where(d => d.UserId == msg.UserId && d.PaymentDate.AddDays(365) > DateTime.UtcNow).ToList();

                    var divsWithNoFx = divs.Where(d => d.FxRate == null);
                    //var missingFxDates = divsWithNoFx.Select(d => d.PaymentDate).ToList();
                    //var rates = _fxRef.Ask<DbOperationResult<Dictionary<DateTime, double>>>(CurrencyMessage.NewGetRatesForDates(Currency.EUR, Currency.USD, ListModule.OfSeq(missingFxDates))).Result;

                    foreach (var d in divsWithNoFx)
                    {
                        //var rate = rates.Item[d.PaymentDate];
                        var rate = await _fxSrv.GetFxRate(d.PaymentDate, Currency.EUR, d.Currency); //TODO: Dont hardcode EUR to maybe someday support other home currencies
                        d.FxRate = rate;
                    }

                    var divsDto = divs.Select(d => d.ToDTO()).OrderBy(d => d.PaymentDate).ToList();
                    var result = new DbOperationResult<List<ReceivedDividendDTO>>(divsDto);
                    Sender.Tell(result);
                }
            });

            Receive<DividendMessage.DeleteReceivedDividend>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();

                    var div = ctx.Dividends.FirstOrDefault(d => d.UserId == msg.UserId && d.Id == msg.DividendId);
                    if (div == null)
                    {
                        Sender.Tell(new DbOperationResult<List<ReceivedDividendDTO>>("Dividend payment not found or access denied"));
                        return;
                    }

                    ctx.Dividends.Remove(div);
                    var divs = ctx.Dividends.Where(d => d.UserId == msg.UserId).ToList();

                    var divsDto = divs.Select(d => d.ToDTO()).ToList();
                    var result = new DbOperationResult<List<ReceivedDividendDTO>>(divsDto);
                    Sender.Tell(result);
                }
            });

            Receive<DividendMessage.ProcessNordnetCsv>(msg =>
            {
                //Note: Assumes user's home currency to be EUR
                var lines = msg.csvLines.ToList();

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    try
                    {
                        foreach (var line in lines)
                        {
                            var fields = line.Split(';');
                            if (fields.Length < 20)
                                continue;

                            var type = fields[4];
                            var invalidatedDate = fields[20];
                            if (type != "OSINKO" || invalidatedDate != "")
                                continue;
                            //TODO: Reduce hardcoding here?
                            var dateStr = fields[1];
                            var tickerStr = fields[5];
                            var sharesStr = fields[8];
                            var divPerShareStr = fields[9];
                            var fxRateStr = fields[18];
                            var currencyStr = fields[13];
                            var totalStr = fields[12].Trim();

                            var date = DateTime.Parse(dateStr);
                            Currency currency;
                            var curValid = Enum.TryParse<Currency>(currencyStr, out currency);
                            double? rate = null;
                            if (curValid && currency == Currency.EUR)
                                rate = 1.0;
                            else if (curValid)
                            {
                                bool rateOk = double.TryParse(fxRateStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var nordnetRate);
                                if (rateOk)
                                    rate = 1 / nordnetRate;
                            }
                                //rate = _fxRef.Ask<double?>(CurrencyMessage.NewGetRateForDay(currency, date)).Result;
                                
                            var current = ctx.Dividends.FirstOrDefault(cd =>
                                cd.UserId == msg.UserId &&
                                cd.PaymentDate.Day == date.Day &&
                                cd.PaymentDate.Month == date.Month &&
                                cd.PaymentDate.Year == date.Year &&
                                cd.CompanyTicker == tickerStr
                                );
                            //TODO: päivitä jos tarpeen tai lisää uusi
                            if (current != null)
                            {
                                //Päivitä arvot?
                            }
                            else
                            {
                                var dividend = new ReceivedDividend();
                                dividend.AmountPerShare = Convert.ToDouble(divPerShareStr.Replace(',', '.'), CultureInfo.InvariantCulture);
                                dividend.CompanyTicker = tickerStr;
                                dividend.Currency = currency;
                                dividend.FxRate = rate;
                                dividend.ShareCount = Convert.ToInt32(sharesStr);
                                dividend.PaymentDate = date;
                                dividend.UserId = msg.UserId;
                                dividend.TotalReceived = Convert.ToDouble(totalStr.Replace(',','.'), CultureInfo.InvariantCulture);

                                ctx.Dividends.Add(dividend);
                            }
                        }
                        ctx.SaveChanges();
                        //TODO: Jokin oma palautustyyppi josta kävisi ilmi lisättyjen/modattujen määrät
                        Sender.Tell(new DbOperationResult<bool>(true));
                    }
                    catch (Exception e)
                    {
                        Sender.Tell(new DbOperationResult<bool>(e.Message));
                    }
                }
            });

            /*
            //TODO: Fix to actually process IB-format xml, not csv
            Receive<DividendMessage.ProcessIBCsv>(msg =>
            {
                //Note: Assumes user's home currency to be EUR
                var lines = msg.csvLines.ToList();

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    try
                    {               
                        
                        foreach (var line in lines)
                        {
                            var fields = line.Split(',');
                            if (fields.Length < 2)
                                continue;
                            var type = fields[0];
                            if (fields[0] != "Dividends" || fields[1] != "Data")
                                continue;

                            //TODO: Reduce hardcoding here?
                            var dateStr = fields[2];
                            var tickerStr = fields[4];
                            var sharesStr = fields[5];
                            var divPerShareStr = fields[6];

                            var date = DateTime.Parse(dateStr);
                            Currency currency;
                            var curValid = Enum.TryParse<Currency>(currencyStr, out currency);
                            double? rate = null;
                            if (curValid && currency == Currency.EUR)
                                rate = 1.0;
                            else if (curValid)
                            {
                                bool rateOk = double.TryParse(fxRateStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var nordnetRate);
                                if (rateOk)
                                    rate = 1 / nordnetRate;
                            }
                            //rate = _fxRef.Ask<double?>(CurrencyMessage.NewGetRateForDay(currency, date)).Result;

                            var current = ctx.Dividends.FirstOrDefault(cd =>
                                cd.UserId == msg.UserId &&
                                cd.PaymentDate.Day == date.Day &&
                                cd.PaymentDate.Month == date.Month &&
                                cd.PaymentDate.Year == date.Year &&
                                cd.CompanyTicker == tickerStr
                                );
                            //TODO: päivitä jos tarpeen tai lisää uusi
                            if (current != null)
                            {
                                //Päivitä arvot?
                            }
                            else
                            {
                                var dividend = new ReceivedDividend();
                                dividend.AmountPerShare = Convert.ToDouble(divPerShareStr.Replace(',', '.'), CultureInfo.InvariantCulture);
                                dividend.CompanyTicker = tickerStr;
                                dividend.Currency = currencyStr;
                                dividend.FxRate = rate;
                                dividend.OwnedShares = Convert.ToInt32(sharesStr);
                                dividend.PaymentDate = date;
                                dividend.UserId = msg.UserId;

                                ctx.Dividends.Add(dividend);
                            }
                        }
                        ctx.SaveChanges();
                        //TODO: Jokin oma palautustyyppi josta kävisi ilmi lisättyjen/modattujen määrät
                        Sender.Tell(new DbOperationResult<bool>(true));
                    }
                    catch (Exception e)
                    {
                        Sender.Tell(new DbOperationResult<bool>(e.Message));
                    }
                }
            });*/

            
            Receive<DividendMessage.ProcessGeneralCsv>(msg =>
            {
                //Note: Assumes user's home currency to be EUR
                var lines = msg.csvLines.ToList();

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    try
                    {               
                        
                        foreach (var line in lines)
                        {
                            var fields = line.Split(',');
                            if (fields.Length < 5)
                                continue;

                            //TODO: First row could contain the order of expected columns, then the data doesnt need to be in certain order
                            var dateStr = fields[0];
                            var symbol = fields[1];
                            var currencyStr = fields[2];
                            var sharesStr = fields[3];
                            var divPerShareStr = fields[4];

                            var date = DateTime.Parse(dateStr);
                            var curValid = Enum.TryParse<Currency>(currencyStr, out var currency);
                            double? rate = null;
                            if (curValid && currency == Currency.EUR)
                                rate = 1.0;
                            else if (!curValid)
                            {
                                Sender.Tell(new DbOperationResult<bool>($"Invalid currency in data: {currencyStr}"));
                                return;
                            }

                            var current = ctx.Dividends.FirstOrDefault(cd =>
                                cd.UserId == msg.UserId &&
                                cd.PaymentDate.Day == date.Day &&
                                cd.PaymentDate.Month == date.Month &&
                                cd.PaymentDate.Year == date.Year &&
                                cd.Symbol == symbol
                                );
                            //TODO: päivitä jos tarpeen tai lisää uusi
                            if (current != null)
                            {
                                //Päivitä arvot?
                            }
                            else
                            {
                                var dividend = new ReceivedDividend();
                                dividend.AmountPerShare = Convert.ToDouble(divPerShareStr.Replace(',', '.'), CultureInfo.InvariantCulture);
                                var stock = ctx.Stocks.FirstOrDefault(s => s.Ticker == symbol);
                                dividend.CompanyTicker = stock != null ? stock.Ticker : null;
                                dividend.Currency = currency;
                                dividend.FxRate = rate;
                                dividend.ShareCount = Convert.ToInt32(sharesStr);
                                dividend.PaymentDate = date;
                                dividend.UserId = msg.UserId;

                                ctx.Dividends.Add(dividend);
                            }
                        }
                        ctx.SaveChanges();
                        //TODO: Jokin oma palautustyyppi josta kävisi ilmi lisättyjen/modattujen määrät
                        Sender.Tell(new DbOperationResult<bool>(true));
                    }
                    catch (Exception e)
                    {
                        Sender.Tell(new DbOperationResult<bool>(e.Message));
                    }
                }
            });

            //TODO: Fix to actually process IB-format xml, not csv
            Receive<DividendMessage.ProcessIBXml>(msg =>
            {
                //Note: Assumes user's home currency to be EUR
                var lines = msg.csvLines.ToList();
                var userId = msg.UserId;

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    try
                    {
                        var textReader = new StringReader(string.Join("\r\n", lines));

                        XmlDocument doc = new XmlDocument();
                        doc.Load(textReader);
                        var root = doc.DocumentElement;
                        var cashTransactionsNodes = root.GetElementsByTagName("CashTransactions");

                        if (cashTransactionsNodes.Count != 1)
                            throw new ArgumentException("Invalid xml");
                        var cashTransactionsNode = cashTransactionsNodes.Item(0);
                        var cashTransactions = cashTransactionsNode.ChildNodes;

                        List<DateTime> paymentDates = new List<DateTime>(); //Collect all payment dates, ask for fx-rate update for all rates at end
                        for (int i = 0; i < cashTransactions.Count; i++)
                        {
                            var cashTransaction = cashTransactions.Item(i);
                            var type = cashTransaction.Attributes.GetNamedItem("type");
                            if (type.InnerText == "Dividends")
                            {
                                var dateStr = cashTransaction.Attributes.GetNamedItem("dateTime").InnerText;
                                var date = DateTime.ParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                var symbol = cashTransaction.Attributes.GetNamedItem("symbol").InnerText;
                                var ticker = ctx.Stocks.FirstOrDefault(s => s.Ticker == symbol)?.Ticker;
                                var currencyStr = cashTransaction.Attributes.GetNamedItem("currency").InnerText;
                                var currency = (Currency)Enum.Parse(typeof(Currency), currencyStr);
                                var description = cashTransaction.Attributes.GetNamedItem("description").InnerText;
                                var totalStr = cashTransaction.Attributes.GetNamedItem("amount").InnerText;
                                var totalAmount = double.Parse(totalStr, CultureInfo.InvariantCulture);
                                var amountPerShare = DetermineIbAmountPerShare(description, totalAmount);
                                int? shares = null;
                                if (amountPerShare != null)
                                    shares = Convert.ToInt32(Math.Round(totalAmount / (double)amountPerShare));

                                paymentDates.Add(date);

                                var current = ctx.Dividends.FirstOrDefault(cd =>
                                   cd.UserId == userId &&
                                   cd.PaymentDate.Day == date.Day &&
                                   cd.PaymentDate.Month == date.Month &&
                                   cd.PaymentDate.Year == date.Year &&
                                   cd.Symbol == symbol
                                );
  
                                if (current != null)
                                {
                                    //Päivitä arvot?
                                }
                                else
                                {
                                    var dividend = new ReceivedDividend(userId, date, ticker, symbol, shares, amountPerShare, totalAmount, currency, Broker.InteractiveBrokers, null);
                                    ctx.Dividends.Add(dividend);
                                }
                            }
                        }
                        ctx.SaveChanges();
                        //TODO: Jokin oma palautustyyppi josta kävisi ilmi lisättyjen/modattujen määrät
                        Sender.Tell(new DbOperationResult<bool>(true));

                        var dates = paymentDates.ToList().OrderBy(p => p);
                        //_fxRef.Tell(CurrencyMessage.NewUpdateOfficialRates(dates.First(), dates.Last()));
                    }
                    catch (Exception e)
                    {
                        Sender.Tell(new DbOperationResult<bool>(e.Message));
                    }
                }
            });
        }

        private double? DetermineIbAmountPerShare(string dividendDescription, double amountTotal)
        {
            //Examples:
            //JNJ (US4781601046) CASH DIVIDEND USD 0.90000000 (Ordinary Dividend)
            //UNA.RTS(DRIPSUNA1805) EXPIRE DIVIDEND RIGHT (Ordinary Dividend)
            //UNA(NL0000009355) CASH DIVIDEND 0.38720000 EUR PER SHARE (Ordinary Dividend)
            var lineWords = dividendDescription.Split(' ').ToList();
            var dividendIndex = lineWords.IndexOf("DIVIDEND");
            var possibleCurrency = lineWords[dividendIndex + 1];
            var isCurrency = Enum.TryParse(typeof(Currency), possibleCurrency, out var result);
            if (isCurrency)
            {
                var amountPerShareStr = lineWords[dividendIndex + 2];
                double amountPerShare = double.Parse(amountPerShareStr, CultureInfo.InvariantCulture);
                return amountPerShare;
                //var shares = Math.Round(amountTotal / amountPerShare);
                //return Convert.ToInt32(shares);
            }
            else //wasnt currency -> was the amount per share, or the UNA etc line with not enough information
            {
                var wasAmount = double.TryParse(possibleCurrency, NumberStyles.Any, CultureInfo.InvariantCulture, out var amountPerShare);
                if (wasAmount)
                {
                    return amountPerShare;
                    //var shares = Math.Round(amountTotal / amountPerShare);
                    //return Convert.ToInt32(shares);
                }
                return null; //Couldnt parse the amount per share -> cannot determine share count
            }
        }
    }
}
