using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{

    public enum Currency
    {
        USD = 1,
        EUR = 2,
        CAD = 3,
        DKK = 4,
        GBP = 5
    }

    public enum Exchange
    {
        NyseNasdaq = 1,
        TSX = 2,
        Helsinki = 3,
        Amsterdam = 4,
        Frankfurt = 5,
        Copenhagen = 6,
    }

    public enum Broker
    {
        Nordnet = 1,
        InteractiveBrokers = 2,
    }

    public enum PriceSource
    {
        Unknown = 1,
        Yahoo = 2,
        QuandlHelsinki = 3,
        AlphaVantage = 4,
        Kauppalehti = 5,
        IEXCloud = 6,
        FMP = 7,
        RapidApiStockDataYFAlternative = 8,
    };

    public enum DataUpdateSource
    {
        Unknown = 1,
        US_CCC = 2,
        Canadian_CCC = 3,
        Manual = 4, //Also when company drops from ccc due to dividend cut etc
        RapidApiStockDataYFAlternative = 5,
        RapidApiYHFinance = 6,
        AlphaVantage = 7,
        FMP = 8,
        IEXCloud = 9
    };
    
    public enum StockAttribute
    {
        Price = 1,
        Dividend = 2,
        EpsTtm = 3,
        DgYears = 4,
        DeptToEquity = 5,
        DivGrowth1 = 6,
        DivGrowth3 = 7,
        DivGrowth5 = 8,
        DivGrowth10 = 9,
        EpsGrowth5 = 11,
        //...
    }
}
