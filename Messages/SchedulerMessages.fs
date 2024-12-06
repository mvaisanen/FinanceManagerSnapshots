namespace Messages


type SchedulerMessage =
    | UpdateRelevantStockPrices of reportBack: bool
    | UpdateAllStockPrices of reportBack: bool
    | DoCurrencyUpdates of backInDays: int
    | CurrencyUpdatesDone of succeeded:bool

