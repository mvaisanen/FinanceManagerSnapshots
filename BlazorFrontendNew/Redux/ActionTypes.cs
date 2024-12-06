using BlazorFrontendNew.BlazorRedux;
using Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFrontendNew.Redux
{
    public class IncrementByOneAction : IAction
    {
    }

    public class IncrementByValueAction : IAction
    {
        public IncrementByValueAction(int value)
        {
            Value = value;
        }

        public int Value { get; set; }
    }

    public class ClearWeatherAction : IAction
    {
    }

    public class LoginStartedAction: IAction { }

    public class LoginFailedAction: IAction
    {
        public string Reason { get; }
        public LoginFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class LoginSucceededAction : IAction
    {
        public string Jwt { get; }
        public LoginSucceededAction(string jwt)
        {
            Jwt = jwt;
        }
    }

    public class LogoutAction : IAction
    {
        public LogoutAction()
        {
        }
    }


    public class WatchlistFetchSucceededAction : IAction
    {
        public WatchlistDTO Watchlist { get; }
        public WatchlistFetchSucceededAction(WatchlistDTO watchlist)
        {
            Watchlist = watchlist;
        }
    }

    public class WatchlistFetchFailedAction : IAction
    {
        public string Reason { get; }
        public WatchlistFetchFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class RemoveFromWatchlistSucceededAction : IAction
    {
        public WatchlistDTO Watchlist { get; }
        public RemoveFromWatchlistSucceededAction(WatchlistDTO watchlist)
        {
            Watchlist = watchlist;
        }
    }

    public class RemoveFromWatchlistFailedAction : IAction
    {
        public string Reason { get; }
        public RemoveFromWatchlistFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class SaveWatchlistStockSucceededAction : IAction
    {
        public WatchlistDTO Watchlist { get; }
        public SaveWatchlistStockSucceededAction(WatchlistDTO watchlist)
        {
            Watchlist = watchlist;
        }
    }

    public class SaveWatchlistStockFailedAction : IAction
    {
        public string Reason { get; }
        public SaveWatchlistStockFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class PortfolioFetchSucceededAction : IAction
    {
        public PortfolioDto Portfolio { get; }
        public PortfolioFetchSucceededAction(PortfolioDto portfolio)
        {
            Portfolio = portfolio;
        }
    }

    public class PortfolioFetchFailedAction : IAction
    {
        public string Reason { get; }
        public PortfolioFetchFailedAction(string reason)
        {
            Reason = reason;
        }
    }


    public class AddPurchaseSucceededAction : IAction
    {
        public PortfolioDto Portfolio { get; }
        public AddPurchaseSucceededAction(PortfolioDto portfolio)
        {
            Portfolio = portfolio;
        }
    }

    public class AddPurchaseFailedAction : IAction
    {
        public string Reason { get; }
        public AddPurchaseFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class PurchaseToEditChangedAction : IAction
    {
        public int PurchaseIdToEdit { get; }
        public PurchaseToEditChangedAction(int purchaseIdToEdit)
        {
            PurchaseIdToEdit = purchaseIdToEdit;
        }
    }

    public class EditPurchaseSucceededAction : IAction
    {
        public PortfolioDto Portfolio { get; }
        public EditPurchaseSucceededAction(PortfolioDto portfolio)
        {
            Portfolio = portfolio;
        }
    }

    public class EditPurchaseFailedAction : IAction
    {
        public string Reason { get; }
        public EditPurchaseFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class DeletePortfolioPositionSucceededAction : IAction
    {
        public PortfolioDto Portfolio { get; }
        public DeletePortfolioPositionSucceededAction(PortfolioDto portfolio)
        {
            Portfolio = portfolio;
        }
    }

    public class DeletePortfolioPositionFailedAction : IAction
    {
        public string Reason { get; }
        public DeletePortfolioPositionFailedAction(string reason)
        {
            Reason = reason;
        }
    }


    public class FxRatesFetchSucceededAction : IAction
    {
        public CurrencyRatesDto FxRates { get; }
        public FxRatesFetchSucceededAction(CurrencyRatesDto fxRates)
        {
            FxRates = fxRates;
        }
    }

    public class FxRatesFetchFailedAction : IAction
    {
        public string Reason { get; }
        public FxRatesFetchFailedAction(string reason)
        {
            Reason = reason;
        }
    }



    public class DividendsFetchSucceededAction : IAction
    {
        public List<ReceivedDividendDTO> Dividends { get; }
        public DividendsFetchSucceededAction(List<ReceivedDividendDTO> dividends)
        {
            Dividends = dividends;
        }
    }

    public class DividendsFetchFailedAction : IAction
    {
        public string Reason { get; }
        public DividendsFetchFailedAction(string reason)
        {
            Reason = reason;
        }
    }


    public class IbPortfolioUploadSucceededAction : IAction
    {
        public PortfolioDto Portfolio { get; }
        public IbPortfolioUploadSucceededAction(PortfolioDto portfolio)
        {
            Portfolio = portfolio;
        }
    }

    public class IbPortfolioUploadFailedAction : IAction
    {
        public string Reason { get; }
        public IbPortfolioUploadFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class IbDividendsUploadSucceededAction : IAction
    {
        public DividendsChangedDto DividendChanges { get; }
        public IbDividendsUploadSucceededAction(DividendsChangedDto changes)
        {
            DividendChanges = changes;
        }
    }

    public class IbDividendsUploadFailedAction : IAction
    {
        public string Reason { get; }
        public IbDividendsUploadFailedAction(string reason)
        {
            Reason = reason;
        }
    }


    public class NordnetDividendsUploadSucceededAction : IAction
    {
        public DividendsChangedDto DividendChanges { get; }
        public NordnetDividendsUploadSucceededAction(DividendsChangedDto changes)
        {
            DividendChanges = changes;
        }
    }

    public class NordnetDividendsUploadFailedAction : IAction
    {
        public string Reason { get; }
        public NordnetDividendsUploadFailedAction(string reason)
        {
            Reason = reason;
        }
    }


    public class DividendPlanFetchStartedAction : IAction
    {
        public DividendPlanFetchStartedAction()
        {
        }
    }
    public class DividendPlanFetchSucceededAction : IAction
    {
        public DividendsPlanDto DividendPlan { get; }
        public DividendPlanFetchSucceededAction(DividendsPlanDto dividendPlan)
        {
            DividendPlan = dividendPlan;
        }
    }

    public class DividendPlanFetchFailedAction : IAction
    {
        public string Reason { get; }
        public DividendPlanFetchFailedAction(string reason)
        {
            Reason = reason;
        }
    }

    public class DividendPlanUploadSucceededAction : IAction
    {
        public DividendsPlanDto DividendPlan { get; }
        public DividendPlanUploadSucceededAction(DividendsPlanDto dividendPlan)
        {
            DividendPlan = dividendPlan;
        }
    }

    public class DividendPlanUploadFailedAction : IAction
    {
        public string Reason { get; }
        public DividendPlanUploadFailedAction(string reason)
        {
            Reason = reason;
        }
    }


}
