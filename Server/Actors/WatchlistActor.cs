using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Common.Dtos;
using Server.Database;
using Server.Mappings;
using Server.Models;
using Financemanager.Server.Database.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers;
using SendGrid.Helpers.Mail;
using FinanceManager.Server.Database;

namespace Server.Actors
{
    public class WatchlistActor: ReceiveActor
    {
        IServiceScopeFactory _serviceScopeFactory;
        ILoggingAdapter _log;
        public WatchlistActor(IServiceScopeFactory serviceScopeFactory)
        {
            _log = Context.GetLogger();
            _log.Debug("WatchlistActor startup...");
            _serviceScopeFactory = serviceScopeFactory;
            Become(Ready);
        }

        private void Ready()
        {
            Receive<WatchlistMessage.GetWatchlist>(msg =>
            {
                using (var scope =  _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var wlist = ctx.Watchlists.Include(wl => wl.WatchlistStocks).ThenInclude(wls => wls.Stock).FirstOrDefault(p => p.Id == msg.Id);
                    //Sender.Tell(wlist.ToDTO());
                    var result = new DbOperationResult<WatchlistDTO>(wlist.ToDTO());
                    Sender.Tell(result);
                }
            });

            Receive<WatchlistMessage.GetWatchlistByUserId>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var wlist = ctx.Watchlists.Include(wl => wl.WatchlistStocks).ThenInclude(wls => wls.Stock).FirstOrDefault(p => p.UserId == msg.UserId);

                    if (wlist != null)
                        Sender.Tell(new DbOperationResult<WatchlistDTO>(wlist.ToDTO()));
                    else
                        Sender.Tell(new DbOperationResult<WatchlistDTO>("No watchlists found for user"));
                    
                }
            });

            Receive<WatchlistMessage.AddOrUpdateStock>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var watchlist = ctx.Watchlists.Include(wl => wl.WatchlistStocks).ThenInclude(ws => ws.Stock).FirstOrDefault(w => w.Id == msg.WatchlistId);
                    if (watchlist == null || watchlist.UserId != msg.UserId)
                    {
                       Sender.Tell(new DbOperationResult<WatchlistDTO>("Invalid watchlist or access denied"));
                       return;
                    }

                    var changed = msg.Stock;
                    var stock = ctx.Stocks.FirstOrDefault(s => s.Id == changed.StockId);
                    if (stock == null)
                    {
                        Sender.Tell(new DbOperationResult<WatchlistDTO>("Invalid stock"));
                        return;
                    }
                    else if (watchlist.WatchlistStocks.Any(current => current.Stock.Ticker == stock.Ticker))
                    {
                        Sender.Tell(new DbOperationResult<WatchlistDTO>($"Stock {msg.Stock.StockTicker} is already on this watchlist"));
                        return;
                    }
                    watchlist.AddOrUpdateStock(stock, changed.TargetPrice, changed.Notify);

                    //wls.RowVersion = changed.RowVersion != null ? changed.RowVersion : new byte[] { };
                    ctx.SaveChanges();
                    Sender.Tell(new DbOperationResult<WatchlistDTO>(true, watchlist.ToDTO()));
                    return;
                }
            });

            Receive<WatchlistMessage.RemoveStockFromWatchlist>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var watchlist = ctx.Watchlists.Include(wl => wl.WatchlistStocks).ThenInclude(ws => ws.Stock).FirstOrDefault(w => w.Id == msg.WatchlistId);
                    if (watchlist == null || watchlist.UserId != msg.UserId)
                    {
                        Sender.Tell(new DbOperationResult<WatchlistDTO>("Invalid watchlist or access denied"));
                        return;
                    }

                    var wls = watchlist.WatchlistStocks.FirstOrDefault(wlstock => wlstock.Id == msg.WatchlistStockId);
                    if (wls == null)
                    {
                        Sender.Tell(new DbOperationResult<WatchlistDTO>("Invalid watchliststock"));
                        return;
                    }

                    //watchlist.Stocks.Remove(wls);
                    //ctx.WatchlistStocks.Remove(wls);
                    watchlist.RemoveFromWatchlist(msg.WatchlistStockId);

                    ctx.SaveChanges();
                    Sender.Tell(new DbOperationResult<WatchlistDTO>(true, watchlist.ToDTO()));
                    return;
                }
            });

            ReceiveAsync<WatchlistMessage.CheckForAlerts>(async msg =>
            {
                _log.Debug("Checking for watchlist stocks below target prices...");

                var apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY", EnvironmentVariableTarget.User);
                _log.Debug("Found sendgrid API key: " + apiKey);
                if (apiKey == null)
                {
                    _log.Error("Failed to retrieve apiKey from Environment settings. Exiting checks.");
                    return;
                }

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var watchlists = ctx.Watchlists.ToList();

                    foreach (var watchlist in watchlists)
                    {
                        var triggeredWatchlistStocks = watchlist.WatchlistStocks
                            .Where(wls => wls.TargetPrice != null && !wls.AlarmSent)
                            .Where(wls => wls.Stock.CurrentPrice < wls.TargetPrice).ToList();

                        if (triggeredWatchlistStocks.Count > 0)
                        {
                            _log.Debug("1 or more stocks triggered, handling sending. Getting user's email...");
                            string email = "";
                            //using (var _repo = new AuthRepository())
                            using (var _authCtx = scope.ServiceProvider.GetService<AuthDbContext>())
                            {
                                var user = _authCtx.Users.FirstOrDefault(u => u.Id == watchlist.UserId);
                                if (user == null)
                                    continue;
                                email = user.Email;
                                if (string.IsNullOrEmpty(email))
                                {
                                    _log.Error("No email set for this user, no email will be send. Continuing to next watchlist...");
                                    continue;
                                }
                            }

                            _log.Debug("Found " + triggeredWatchlistStocks.Count + " stocks below target, formatting message...");
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("The following stocks have reached their target prices:");
                            foreach (var triggered in triggeredWatchlistStocks)
                            {
                                sb.AppendLine(triggered.Stock.Name + ", current price " + triggered.Stock.CurrentPrice);
                            }

                            var client = new SendGridClient(apiKey);

                            //Email from = new Email("no-reply@financialmanager.azurewebsites.net");
                            //string subject = "Stock(s) on watchlist have reached target price!";
                            //Email to = new Email(email);
                            //Content content = new Content("text/plain", sb.ToString());
                            //Mail mail = new Mail(from, subject, to, content);
                            var sgMsg = new SendGridMessage()
                            {
                                From = new EmailAddress("no-reply@financialmanager.azurewebsites.net", "FinanceManager"),
                                Subject = "Stock(s) on FinanceManager watchlist have reached target price!",
                                PlainTextContent = sb.ToString(),
                            };
                            sgMsg.AddTo(new EmailAddress(email));

                            _log.Debug("Sending email...");
                            var response = await client.SendEmailAsync(sgMsg);
                            _log.Debug("Mail sending done. Response status: " + response.StatusCode);
                            if (response.StatusCode != System.Net.HttpStatusCode.OK && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                                _log.Error($"Watchlist notification email sending failed with statuscode {response.StatusCode}");

                            foreach (var trigged in triggeredWatchlistStocks)
                            {
                                trigged.AlarmSent = true; //Flagging as sent even if failed - todo: some sort of third status "sending fail" which would try to be resend at much lower frequency
                            }
                            
                        }
                    }

                    ctx.SaveChanges();
                }              
            });
        }
    }
}
