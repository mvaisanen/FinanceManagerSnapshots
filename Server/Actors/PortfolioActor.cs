using Akka.Actor;
using Akka.Event;
using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Server.Database;
using Microsoft.EntityFrameworkCore;
using Server.Mappings;
using Financemanager.Server.Database.Domain;
using Server.Models;
using Common.Dtos;
using FinanceManager.Server.Database;

namespace Server.Actors
{
    public class PortfolioActor : ReceiveActor
    {
        private ILoggingAdapter _log;
        //private readonly ActorSelection _fxRef;
        IServiceScopeFactory _serviceScopeFactory;

        public PortfolioActor(IServiceScopeFactory serviceScopeFactory)
        {
            _log = Context.GetLogger();
            _log.Info("PortfolioActor started.");
            //_fxRef = FinanceManagerServer.FinanceManagerSystem.ActorSelection("/user/fxrates");

            //System.Diagnostics.Debug.WriteLine("PortfolioActor startup...");
            _serviceScopeFactory = serviceScopeFactory;
            Become(Ready);
        }

        private void Ready()
        {
            Receive<PortfolioMessage.GetPortfolio>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    //var port = ctx.Portfolios.Include(po => po.Positions.Select(p => p.Buys)).Include(po => po.Positions.Select(p => p.Stock)).FirstOrDefault(p => p.Id == msg.Id);
                    var port = ctx.Portfolios
                        .Include(po => po.Positions).ThenInclude(pos => pos.Buys) //Ainakaan tämä rivi ei tällä hetkellä (10.10.19) kelpaa
                        .Include(po => po.Positions).ThenInclude(pos => pos.Stock).ThenInclude(s => s.SSDSafetyScore)
                        .FirstOrDefault(p => p.Id == msg.Id);
                    if (port != null)
                        Sender.Tell(new DbOperationResult<PortfolioDto>(port.ToDTO()));
                    else
                        Sender.Tell(new DbOperationResult<PortfolioDto>("Invalid portfolio id"));
                }
            });

            ReceiveAsync<PortfolioMessage.GetPortfolioByUserId>(async msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    //var wlist = ctx.Watchlists.Include(wl => wl.Stocks).ThenInclude(wls => wls.Stock).FirstOrDefault(p => p.UserId == msg.UserId);
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    //var port = ctx.Portfolios.Include(po => po.Positions.Select(p => p.Buys)).Include(po => po.Positions.Select(p => p.Stock)).FirstOrDefault(po => po.UserId == msg.UserId);
                    var port = ctx.Portfolios
                        .Include(po => po.Positions).ThenInclude(pos => pos.Buys) 
                        .Include(po => po.Positions).ThenInclude(pos => pos.Stock).ThenInclude(s => s.SSDSafetyScore)
                        .Include(po => po.Positions).ThenInclude(pos => pos.Stock).ThenInclude(s => s.StockDataUpdateSources)
                        .FirstOrDefault(p => p.UserId == msg.UserId);


                    if (port == null)
                    {
                        Sender.Tell(new DbOperationResult<PortfolioDto>("No portfolios found for user"));
                        return;
                    }
                    else
                    {
                        //TODO: Add rates later, or move them into new model
                        //var rateUsd = await _fxRef.Ask<double>(CurrencyMessage.NewGetCurrentRate(Currency.USD));
                        //var rateCad = await _fxRef.Ask<double>(CurrencyMessage.NewGetCurrentRate(Currency.CAD));
                        //var rateDkk = await _fxRef.Ask<double>(CurrencyMessage.NewGetCurrentRate(Currency.DKK));
                        var portDto = port.ToDTO();
                        /*portDto.FxRates = new Dictionary<Currency, double>()
                        {
                            {Currency.USD, rateUsd },
                            {Currency.CAD, rateCad },
                            {Currency.DKK, rateDkk }
                        };*/

                        Sender.Tell(new DbOperationResult<PortfolioDto>(portDto));
                    }
                }
            });


            Receive<PortfolioMessage.AddOrUpdate>(msg =>
            {
                var portDTO = msg.PortfolioDto;
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var portfolio = ctx.Portfolios.FirstOrDefault(b => b.Id == portDTO.Id);
                    if (portfolio == null)
                    {
                        portfolio = new Portfolio(userId: msg.UserId);
                        ctx.Portfolios.Add(portfolio);
                    }
                    else if (portfolio.UserId != msg.UserId) //dont allow updating different user's portfolio! TODO: Allow admin?
                    {
                        Sender.Tell(null);
                        return;
                    }



                    //Remove deleted single buys. TODO: Unless its the only buy, in which case remove position?
                    // Go through all positions, find all buys that have been removed from that positions in the DTO and delete them from ctx
                    //portfolio.Positions.ForEach(position =>
                    //    position.Buys.Where(buy =>
                    //        !portDTO.Positions.First(p =>
                    //            p.Id == position.Id).Buys.Any(b =>
                    //                b.Id == buy.Id)).Select(deleted => ctx.StockPurchases.Remove(deleted)));
                    var deletedBuys = portfolio.Positions.SelectMany(p => p.Buys).Where(b => !portDTO.Positions.Any(pos => pos.Buys.Any(buy => buy.Id == b.StockPurchaseId)));
                    deletedBuys.ToList().ForEach(b => portfolio.RemoveStockPurchase(b.StockPurchaseId));

                    //Remove deleted portfolio positions. TODO: This fails if done before removing single buys, need to remove deleted stock purchases first!
                    portfolio.Positions.Where(pos => !portDTO.Positions.Any(posDTO => posDTO.Id == pos.PortfolioPositionId))
                        //.ToList().ForEach(deleted => ctx.PortfolioPositions.Remove(deleted));
                        .ToList().ForEach(deleted => portfolio.RemovePosition(deleted.PortfolioPositionId));

                    //Update or add Positions and Buys
                    foreach (var positionDTO in portDTO.Positions)
                    {
                        //var position = portfolio.Positions.SingleOrDefault(s => s.Id == positionDTO.Id);
                        //if (position == null) //dto has added new position
                        //{
                        //    position = new PortfolioPosition();
                        //    var stock = ctx.Stocks.FirstOrDefault(st => st.Id == positionDTO.Stock.Id);
                        //    if (stock == null)
                        //    {
                        //        Sender.Tell(null);
                        //        return;
                        //    }
                        //    position.Stock = stock;
                        //    portfolio.Positions.Add(position);
                        //}

                        // Update subFoos that are in the newFoo.SubFoo collection   
                        foreach (var buyDTO in positionDTO.Buys)
                        {
                            var buy = portfolio.Positions.SelectMany(pos => pos.Buys).FirstOrDefault(b => b.StockPurchaseId == buyDTO.Id);
                            if (buy == null) //DTO has new added buy
                            {
                                //position.Buys.Add(buy);
                                var stock = ctx.Stocks.SingleOrDefault(s => s.Id == positionDTO.Stock.Id);
                                portfolio.AddStockPurchase(stock, buyDTO.PurchaseDate, buyDTO.Amount, buyDTO.Price);
                            }
                            else
                            {
                                //buy.Amount = buyDTO.Amount;
                                //buy.Price = buyDTO.Price;
                                //buy.PurchaseDate = buyDTO.PurchaseDate;
                                portfolio.UpdatePurchase(buy.StockPurchaseId, buyDTO.PurchaseDate, buy.Amount, buyDTO.Price);
                            }
                        }
                    }
                    ctx.SaveChanges();
                    Sender.Tell(portfolio.ToDTO());
                }
            });


            Receive<PortfolioMessage.AddToPortfolio>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var purchaseToAdd = msg.purchase;
                    //var watchlist = ctx.Watchlists.Include(wl => wl.Stocks).ThenInclude(ws => ws.Stock).FirstOrDefault(w => w.Id == msg.WatchlistId);
                    var portfolio = ctx.Portfolios
                        .Include(pf => pf.Positions).
                            ThenInclude(pos => pos.Buys)
                        .Include(pf => pf.Positions)
                            .ThenInclude(pos => pos.Stock)
                        .FirstOrDefault(p => p.Id == purchaseToAdd.PortfolioId);

                    _log.Debug($"Receive<PortfolioMessage.AddToPortfolio>: portfolio is null={portfolio == null}, " +
                        $"portfolio userid={(portfolio != null ? portfolio.UserId : null)}, msg userId={msg.userId}, purchasePortfolioId={purchaseToAdd.PortfolioId}");

                    if (portfolio == null || portfolio.UserId != msg.userId) //if portfolio has different userid than the user that made the request, deny add
                    {
                        Sender.Tell(new DbOperationResult<PortfolioDto>("Invalid portfolio or access denied"));
                        return;
                    }

                    if (purchaseToAdd.Price <= 0.0001)
                    {
                        Sender.Tell(new DbOperationResult<PortfolioDto>("Invalid price"));
                        return;
                    }
                    if (purchaseToAdd.PurchaseDate < new DateTime(1920, 1, 1))
                    {
                        Sender.Tell(new DbOperationResult<PortfolioDto>("Invalid purchase date"));
                        return;
                    }

                    var stock = ctx.Stocks.FirstOrDefault(s => s.Ticker == purchaseToAdd.StockTicker.ToUpper());
                    if (stock == null)
                    {
                        Sender.Tell(new DbOperationResult<PortfolioDto>("Invalid stock ticker"));
                        return;
                    }

                    //var position = portfolio.Positions.FirstOrDefault(pos => pos.Stock.Id == stock.Id); //Do we have existing position (old buys) for this stock purchase?
                    //if (position == null)
                    //{
                    //    position = new PortfolioPosition();
                    //    position.Stock = stock;
                    //    portfolio.Positions.Add(position);
                    //}
                    //
                    //var buy = new StockPurchase();
                    //buy.Amount = purchaseToAdd.Amount;
                    //buy.Price = purchaseToAdd.Price;
                    //buy.PurchaseDate = purchaseToAdd.PurchaseDate;
                    //position.Buys.Add(buy);

                    portfolio.AddStockPurchase(stock, purchaseToAdd.PurchaseDate, purchaseToAdd.Amount, purchaseToAdd.Price);

                    ctx.SaveChanges();
                    Sender.Tell(new DbOperationResult<PortfolioDto>(portfolio.ToDTO()));
                }

            });

            Receive<PortfolioMessage.UpdatePurchase>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    //var portfolio = ctx.Portfolios.FirstOrDefault(p => p.Positions.Any(pos => pos.Buys.Any(buy => buy.Id == msg.purchase.Id)));
                    var portfolio = ctx.Portfolios
                        .Include(pf => pf.Positions).
                            ThenInclude(pos => pos.Buys)
                        .Include(pf => pf.Positions)
                            .ThenInclude(pos => pos.Stock)
                        .FirstOrDefault(p => p.Positions.Any(pos => pos.Buys.Any(buy => buy.StockPurchaseId == msg.purchase.Id)));
                    if (portfolio == null || portfolio.UserId != msg.userId) //Dont let users edit portfolio they dont own, and ofc some portfolio must have this purchase
                    {
                        Sender.Tell(new DbOperationResult<PortfolioDto>("Invalid purchase to edit"));
                        return;
                    }

                    //Already checked above that this exists
                    //var dbPurchase = ctx.StockPurchases.FirstOrDefault(pur => pur.Id == msg.purchase.Id);
                    //var dbPurchase = portfolio.Positions.SelectMany(pos => pos.Buys).FirstOrDefault(buy => buy.Id == msg.purchase.Id);
                    //dbPurchase.Amount = msg.purchase.Amount;
                    //dbPurchase.Price = msg.purchase.Price;
                    //dbPurchase.PurchaseDate = msg.purchase.PurchaseDate;
                    portfolio.UpdatePurchase(msg.purchase.Id, msg.purchase.PurchaseDate, msg.purchase.Amount, msg.purchase.Price);

                    ctx.SaveChanges();
                    Sender.Tell(new DbOperationResult<PortfolioDto>(true, portfolio.ToDTO()));
                }
            });


            Receive<PortfolioMessage.RemoveFromPortfolio>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var portfolio = ctx.Portfolios.Include(p => p.Positions).FirstOrDefault(p => p.Id == msg.portfolioId);

                    if (portfolio == null || portfolio.UserId != msg.userId) //if portfolio has different userid than the user that made the request, deny add
                    {
                        Sender.Tell(null);
                        return;
                    }

                    var position = portfolio.Positions.FirstOrDefault(pos => pos.PortfolioPositionId == msg.positionId);
                    if (position == null)
                    {
                        Sender.Tell(null);
                        return;
                    }

                    //When removing position, remove all buys for it as well
                    //foreach (var buy in position.Buys.ToList()) //ToList to prevent enumeration error when medifying underlying object
                    //{
                    //    ctx.StockPurchases.Remove(buy);
                    //}
                    //
                    //ctx.PortfolioPositions.Remove(position);

                    portfolio.RemovePosition(msg.positionId);

                    ctx.SaveChanges();
                    Sender.Tell(portfolio.ToDTO());
                }
            });

            
            Receive<PortfolioMessage.DeleteBuy>(msg =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetService<FinanceManagerContext>();
                    var portfolio = ctx.Portfolios.FirstOrDefault(p => p.Id == msg.portfolioId);

                    if (portfolio == null || portfolio.UserId != msg.userId) //if portfolio has different userid than the user that made the request, deny add
                    {
                        Sender.Tell(new DbOperationResult<PortfolioDto>(false, null, "Invalid or unauthorized portfolio id"));
                        return;
                    }

                    var position = portfolio.Positions.FirstOrDefault(p => p.Buys.Any(b => b.StockPurchaseId == msg.BuyId));
                    if (position == null)
                    {                       
                        Sender.Tell(new DbOperationResult<PortfolioDto>(false, null, "No such purchase exists"));
                        return;
                    }

                    //var isOnlyBuy = position.Buys.Count <= 1; //if this was the only buy for a position, remove entire position
                    //var buy = position.Buys.FirstOrDefault(b => b.Id == msg.BuyId);
                    //position.Buys.Remove(buy);
                    //ctx.StockPurchases.Remove(buy);
                    //if (isOnlyBuy)
                    //{
                    //    portfolio.Positions.Remove(position);
                    //    ctx.PortfolioPositions.Remove(position);
                    //}

                    portfolio.RemoveStockPurchase(msg.BuyId);

                    ctx.SaveChanges();
                    Sender.Tell(new DbOperationResult<PortfolioDto>(true, portfolio.ToDTO()));
                }

            });

        }

        /*
        T UnProxy<T>(DbContext context, T proxyObject) where T : class
        {
            var proxyCreationEnabled = context.Configuration.ProxyCreationEnabled;
            try
            {
                context.Configuration.ProxyCreationEnabled = false;
                T poco = context.Entry(proxyObject).CurrentValues.ToObject() as T;
                return poco;
            }
            finally
            {
                context.Configuration.ProxyCreationEnabled = proxyCreationEnabled;
            }
        }*/
    }
}
