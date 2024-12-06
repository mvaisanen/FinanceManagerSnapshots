namespace Messages

open Common.Dtos

type WatchlistMessage =
    | GetWatchlist of Id:int
    | GetWatchlistByUserId of UserId:string
    | AddOrUpdate of UserId:string * Watchlist:WatchlistDTO
    | AddOrUpdateStock of UserId:string * WatchlistId:int * Stock:WatchlistStockDTO
    | RemoveStockFromWatchlist of UserId: string * WatchlistId: int * WatchlistStockId: int
   // | AddToWatchlist of UserId:string * WLStock:AddToWatchlistModel
    | CheckForAlerts of really:bool

