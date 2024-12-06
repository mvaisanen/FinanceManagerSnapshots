namespace Messages

open Common.Dtos

type DividendMessage =
    | GetDividendsByUserId of UserId:string
    | GetTtmDividends of UserId:string
    | ProcessNordnetCsv of csvLines:List<string> * UserId:string
    | ProcessIBXml of csvLines:List<string> * UserId:string
    | ProcessIBCsv of csvLines:List<string> * UserId:string
    | ProcessGeneralCsv of csvLines:List<string> * UserId:string
    | DeleteReceivedDividend of UserId:string * DividendId: int


