namespace Messages

open Common.Dtos
open System
open Common.HelperModels

type PortfolioMessage =
    | AddOrUpdate of UserId:string * PortfolioDto:PortfolioDto
    | GetPortfolio of Id:int
    | GetPortfolioByUserId  of UserId:string
    | DeleteBuy of userId:string * portfolioId:int * BuyId:int
    | AddToPortfolio of userId:string * purchase: AddToPortfolioDto
    | UpdatePurchase of userId:string * purchase: StockPurchaseDto
    | RemoveFromPortfolio of userId:string * portfolioId:int * positionId:int

