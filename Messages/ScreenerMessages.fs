namespace Messages

open Common.Dtos

type ScreenerMessage =
    | GetAllChampions of dummy:bool
    | GetSearchResults of minCyoc:double * dgyears:int
    | GetMatches of DgYears:int * Yield:double * Pe:double

