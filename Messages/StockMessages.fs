namespace Messages

open Common.Dtos

type StockMessage =
    | GetStock of Id:int
    | GetClosestMatches of searchParam:string
    | UpdateUsCCC of filewithPath:string
    | UpdateAllStockPrices of reportBack: bool
    | UpdateRelevantStockPrices of reportBack: bool
    | UpdateRelevantIexStockPrices of UpdateId: int
    | UpdateAllIexStockPrices of UpdateId: int
    | UpdateRelevantStockPricesDone of succeeded: bool

