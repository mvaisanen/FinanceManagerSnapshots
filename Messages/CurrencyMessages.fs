namespace Messages

open System
open Common

type CurrencyMessage =
    | UpdateOfficialRates of StartDate:DateTime * EndDate:DateTime
    | UpdateAllCurrentRates of really:bool
    | GetCurrentRateIfAvailable of userId:string
    | GetRatesForDates of FirstCurrency:Currency * SecondCurrency:Currency * Dates:List<DateTime> //EURUSD -> first=EUR, second=USD
    | GetRateForDay of Currency:Currency * Date:DateTime

