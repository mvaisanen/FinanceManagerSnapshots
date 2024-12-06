using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Actors
{
    public delegate IActorRef WatchlistActorProvider();
    public delegate IActorRef PortfolioActorProvider();
    public delegate IActorRef StockActorProvider();
    public delegate IActorRef SchedulerActorProvider();
    public delegate IActorRef DividendActorProvider();
    public delegate IActorRef ScreenerActorProvider();
}
