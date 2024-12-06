using BlazorFrontendNew.BlazorRedux;
using BlazorFrontendNew.Store;
using Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFrontendNew.Redux
{
    public static class Reducers
    {
        public static AppState RootReducer(AppState state, IAction action)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return new AppState
            {
                Location = Location.Reducer(state.Location, action),
                CurrentCounter = CountReducer(state.CurrentCounter, action),
                LoginState = LoginReducer(state.LoginState, action),
                WatchlistState = WatchlistReducer(state.WatchlistState, action),
                PortfolioState = PortfolioReducer(state.PortfolioState, action),
                CurrentFxRates = CurrencyReducer(state, action),
                UserDividends = DividendsReducer(state.UserDividends, action),
                //Forecasts = ForecastsReducer(state.Forecasts, action)
            };
        }

        private static int CountReducer(int count, IAction action)
        {
            switch (action)
            {
                case IncrementByOneAction _:
                    return count + 1;
                case IncrementByValueAction a:
                    return count + a.Value;
                default:
                    return count;
            }
        }

        private static CurrencyRatesDto CurrencyReducer(AppState state, IAction action)
        {
            switch (action)
            {
                case FxRatesFetchFailedAction _:
                    Console.WriteLine("CurrencyReducer handling FxRatesFetchFailedAction");
                    return null;
                case FxRatesFetchSucceededAction a:
                    Console.WriteLine("CurrencyReducer handling FxRatesFetchSucceededAction");
                    return ((FxRatesFetchSucceededAction)action).FxRates;
                default:
                    Console.WriteLine("CurrencyReducer handling default action. Action: " + action.GetType().ToString());
                    return state.CurrentFxRates != null ? state.CurrentFxRates : null; //null sijaan olisiko parempi palauttaa kaikki ratet 1.00 ja date null tms?
            }
        }

        private static LoginState LoginReducer(LoginState state, IAction action)
        {
            if (state == null)
                Console.WriteLine("LoginReducer handling change. Old state is null");

            switch (action)
            {
                case LoginStartedAction _:
                    Console.WriteLine("LoginReducer handling change. New state jwt: null");
                    return new LoginState() { LoginMsg = "Logging in, please wait...", JwtToken = null };
                case LoginSucceededAction a:
                    Console.WriteLine("LoginReducer handling change. New state jwt: "+a.Jwt);
                    return new LoginState() { LoginMsg = "Login succeeded", JwtToken = a.Jwt };
                case LoginFailedAction a:
                    Console.WriteLine("LoginReducer handling change. New state jwt: null");
                    return new LoginState() { LoginMsg = $"Login failed: {a.Reason}", JwtToken = null };
                case LogoutAction a:
                    Console.WriteLine("LoginReducer handling change. New state jwt: null");
                    return new LoginState() { LoginMsg = $"", JwtToken = null };
                default:
                    Console.WriteLine("LoginReducer handling default action. Action: "+action.GetType().ToString());
                    return state != null ? state : new LoginState();
            }
        }

        private static WatchlistState WatchlistReducer(WatchlistState current, IAction action)
        {
            if (current == null)
                Console.WriteLine("WatchlistReducer handling change. Old state is null");

            switch (action)
            {
                case WatchlistFetchSucceededAction a:
                    Console.WriteLine("WatchlistReducer handling WatchlistFetchSucceededAction. Watchlist id="+a.Watchlist.Id);
                    return new WatchlistState() { Watchlist = a.Watchlist, ErrorText=null};
                case WatchlistFetchFailedAction a:
                    Console.WriteLine("WatchlistReducer handling WatchlistFetchFailedAction");
                    return new WatchlistState() { Watchlist = null, ErrorText=a.Reason};
                case RemoveFromWatchlistSucceededAction a:
                    Console.WriteLine("WatchlistReducer handling RemoveFromWatchlistSucceededAction. Watchlist id=" + a.Watchlist.Id);
                    return new WatchlistState() { Watchlist = a.Watchlist, ErrorText = null };
                case RemoveFromWatchlistFailedAction a:
                    Console.WriteLine("WatchlistReducer handling RemoveFromWatchlistFailedAction");
                    return new WatchlistState() { Watchlist = current.Watchlist, ErrorText = "Removing from watchlist failed: "+a.Reason };
                case SaveWatchlistStockSucceededAction a:
                    Console.WriteLine("WatchlistReducer handling SaveWatchlistStockSucceededAction. Watchlist id=" + a.Watchlist.Id);
                    return new WatchlistState() { Watchlist = a.Watchlist, ErrorText = null };
                case SaveWatchlistStockFailedAction a:
                    Console.WriteLine("WatchlistReducer handling SaveWatchlistStockFailedAction");
                    return new WatchlistState() { Watchlist = current.Watchlist, ErrorText = "Modification save failed: "+a.Reason };
                default:
                    Console.WriteLine("WatchlistReducer handling default action. Action: " + action.GetType().ToString());
                    return current != null ? current : new WatchlistState() { Watchlist = null, ErrorText=null};
            }
        }

        private static PortfolioState PortfolioReducer(PortfolioState current, IAction action)
        {
            if (current == null)
                Console.WriteLine("PortfolioReducer handling change. Old state is null");

            switch (action)
            {
                case PortfolioFetchSucceededAction a:
                    Console.WriteLine("PortfolioReducer handling PortfolioFetchSucceededAction. Portfolio id=" + a.Portfolio.Id);
                    return new PortfolioState() { Portfolio = a.Portfolio, ErrorText = null };
                case PortfolioFetchFailedAction a:
                    Console.WriteLine("PortfolioReducer handling PortfolioFetchFailedAction");
                    return new PortfolioState() { Portfolio = null, ErrorText = a.Reason };
                case AddPurchaseSucceededAction a:
                    Console.WriteLine("PortfolioReducer handling AddPurchaseSucceededAction");
                    return new PortfolioState() { Portfolio = a.Portfolio, ErrorText = null };
                case AddPurchaseFailedAction a:
                    Console.WriteLine("PortfolioReducer handling AddPurchaseFailedAction");
                    return new PortfolioState() { Portfolio = current.Portfolio, ErrorText = a.Reason };
                case EditPurchaseSucceededAction a:
                    Console.WriteLine("PortfolioReducer handling EditPurchaseSucceededAction");
                    return new PortfolioState() { Portfolio = a.Portfolio, ErrorText = null, PurchaseIdToEdit = -1 };
                case EditPurchaseFailedAction a:
                    Console.WriteLine("PortfolioReducer handling EditPurchaseFailedAction");
                    return new PortfolioState() { Portfolio = current.Portfolio, ErrorText = a.Reason };
                case PurchaseToEditChangedAction a:
                    Console.WriteLine("PortfolioReducer handling PurchaseToEditChangedAction");
                    return new PortfolioState() { Portfolio = current.Portfolio, ErrorText = current.ErrorText, PurchaseIdToEdit = a.PurchaseIdToEdit };
                case DeletePortfolioPositionSucceededAction a:
                    Console.WriteLine("PortfolioReducer handling DeletePortfolioPositionSucceededAction");
                    return new PortfolioState() { Portfolio = a.Portfolio, ErrorText = current.ErrorText };
                case DeletePortfolioPositionFailedAction a:
                    Console.WriteLine("PortfolioReducer handling DeletePortfolioPositionFailedAction");
                    return new PortfolioState() { Portfolio = current.Portfolio, ErrorText = a.Reason };
                case IbPortfolioUploadSucceededAction a:
                    Console.WriteLine("PortfolioReducer handling IbPortfolioUploadSucceededAction");
                    return new PortfolioState() { Portfolio = a.Portfolio, ErrorText = current.ErrorText };
                case IbPortfolioUploadFailedAction a:
                    Console.WriteLine("PortfolioReducer handling IbPortfolioUploadFailedAction");
                    return new PortfolioState() { Portfolio = current.Portfolio, ErrorText = a.Reason  };

                default:
                    Console.WriteLine("PortfolioReducer handling default action. Action: " + action.GetType().ToString());
                    return current != null ? current : new PortfolioState() { Portfolio = null, ErrorText = null };
            }
        }

        private static DividendsState DividendsReducer(DividendsState current, IAction action)
        {
            if (current == null)
                Console.WriteLine("DividendsReducer handling change. Old state is null");

            switch (action)
            {
                case DividendsFetchSucceededAction a:
                    Console.WriteLine("DividendsReducer handling DividendsFetchSucceededAction");
                    return new DividendsState() { Dividends = a.Dividends, DividendPlan = current.DividendPlan, ErrorText = null };
                case DividendsFetchFailedAction a:
                    Console.WriteLine("DividendsReducer handling DividendsFetchFailedAction");
                    return new DividendsState() {Dividends = new List<ReceivedDividendDTO>(), DividendPlan = current.DividendPlan, ErrorText = a.Reason };
                case IbDividendsUploadSucceededAction a:
                    Console.WriteLine("DividendsReducer handling IbDividendsUploadSucceededAction");
                    return new DividendsState() { Dividends = CalculateNewDividendsState(current, a.DividendChanges), DividendPlan = current.DividendPlan,  ErrorText = null };
                case IbDividendsUploadFailedAction a:
                    Console.WriteLine("DividendsReducer handling IbDividendsUploadFailedAction");
                    return new DividendsState() { Dividends = current.Dividends.ToList(), DividendPlan = current.DividendPlan, ErrorText = a.Reason };
                case NordnetDividendsUploadSucceededAction a:
                    Console.WriteLine("DividendsReducer handling NordnetDividendsUploadSucceededAction");
                    return new DividendsState() { Dividends = CalculateNewDividendsState(current, a.DividendChanges), DividendPlan = current.DividendPlan, ErrorText = null };
                case NordnetDividendsUploadFailedAction a:
                    Console.WriteLine("DividendsReducer handling NordnetDividendsUploadFailedAction");
                    return new DividendsState() { Dividends = current.Dividends.ToList(), DividendPlan = current.DividendPlan, ErrorText = a.Reason };
                case DividendPlanFetchSucceededAction a:
                    Console.WriteLine("DividendsReducer handling DividendPlanFetchSucceededAction");
                    return new DividendsState() { Dividends = current.Dividends, DividendPlan = a.DividendPlan, ErrorText = null };
                case DividendPlanFetchFailedAction a:
                    Console.WriteLine("DividendsReducer handling DividendPlanFetchFailedAction");
                    return new DividendsState() { Dividends = current.Dividends, DividendPlan = current.DividendPlan, ErrorText = a.Reason };
                case DividendPlanUploadSucceededAction a:
                    Console.WriteLine("DividendsReducer handling DividendPlanUploadSucceededAction");
                    return new DividendsState() { Dividends = current.Dividends, DividendPlan = a.DividendPlan, ErrorText = null };
                case DividendPlanUploadFailedAction a:
                    Console.WriteLine("DividendsReducer handling DividendPlanUploadFailedAction");
                    return new DividendsState() { Dividends = current.Dividends.ToList(), DividendPlan = current.DividendPlan, ErrorText = a.Reason };

                default:
                    Console.WriteLine("DividendsReducer handling default action. Action: " + action.GetType().ToString());
                    return current != null ? current : new DividendsState() { Dividends = new List<ReceivedDividendDTO>(), ErrorText = null };
            }
        }

        private static List<ReceivedDividendDTO> CalculateNewDividendsState(DividendsState current, DividendsChangedDto changes)
        {
            var newDivs = current.Dividends.ToList()
                .Where(d => (!changes.DividendsRemoved.Any(r => r.Id == d.Id) && !changes.DividendsAddedOrModified.Any(r => r.Id == d.Id))); //ToList() on purpose to create a NEW list of divs, not to modify old
            return newDivs.Concat(changes.DividendsAddedOrModified).OrderByDescending(d => d.PaymentDate).ToList();
        }
    }
}
