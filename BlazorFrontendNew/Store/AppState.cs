using Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFrontendNew.Store
{
    public class AppState
    {
        public string Location { get; set; }
        public int CurrentCounter { get; set; }
        public LoginState LoginState { get; set; }
        public WatchlistState WatchlistState { get; set; }
        public PortfolioState PortfolioState { get; set; }
        public CurrencyRatesDto CurrentFxRates { get; set; } = null;
        public DividendsState UserDividends { get; set; }
    }

    public class LoginState
    {
        public string LoginMsg { get; set; }
        public string JwtToken { get; set; }
    }

    public class WatchlistState
    {
        public WatchlistDTO Watchlist { get; set; }
        public string ErrorText { get; set; }
    }

    public class PortfolioState
    {
        public PortfolioDto Portfolio { get; set; }
        public string ErrorText { get; set; }
        public int PurchaseIdToEdit { get; set; }
    }

    public class DividendsState
    {
        public List<ReceivedDividendDTO> Dividends { get; set; }
        public DividendsPlanDto DividendPlan { get; set; }

        public string ErrorText { get; set; }
    }
}
