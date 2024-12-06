using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services.StockServices
{
    
    public class RapidApiStockDataYFAlternativeDetailsReply
    {
        public RapidApiStockDataYFAlternativeQuoteSummary quoteSummary { get; set; }
    }

    public class DoubleValueWithRawFmt
    {
        public double raw { get; set; }
        public string fmt { get; set; }
    }

    public class LongValueWithRawFmt
    {
        public long raw { get; set; }
        public string fmt { get; set; }
    }

    public class IntValueWithTwoFormats
    {
        public int raw { get; set; }
        public string fmt { get; set; }
        public string longFmt { get; set; }
    }

    public class LongValueWithTwoFormats
    {
        public long raw { get; set; }
        public string fmt { get; set; }
        public string longFmt { get; set; }
    }

    public class RapidApiStockDataYFAlternativeSummaryDetail
    {
        public int maxAge { get; set; }
        public IntValueWithTwoFormats priceHint { get; set; }
        public DoubleValueWithRawFmt previousClose { get; set; }
        public DoubleValueWithRawFmt open { get; set; }
        public DoubleValueWithRawFmt dayLow { get; set; }
        public DoubleValueWithRawFmt dayHigh { get; set; }
        public DoubleValueWithRawFmt regularMarketPreviousClose { get; set; }
        public DoubleValueWithRawFmt regularMarketOpen { get; set; }
        public DoubleValueWithRawFmt regularMarketDayLow { get; set; }
        public DoubleValueWithRawFmt regularMarketDayHigh { get; set; }
        public DoubleValueWithRawFmt dividendRate { get; set; }
        public DoubleValueWithRawFmt dividendYield { get; set; }
        public LongValueWithRawFmt exDividendDate { get; set; }
        public DoubleValueWithRawFmt payoutRatio { get; set; }
        public DoubleValueWithRawFmt fiveYearAvgDividendYield { get; set; }
        public DoubleValueWithRawFmt beta { get; set; }
        public DoubleValueWithRawFmt trailingPE { get; set; }
        public DoubleValueWithRawFmt forwardPE { get; set; }
        public LongValueWithTwoFormats volume { get; set; }
        public LongValueWithTwoFormats regularMarketVolume { get; set; }
        public LongValueWithTwoFormats averageVolume { get; set; }
        public LongValueWithTwoFormats averageVolume10days { get; set; }
        public LongValueWithTwoFormats averageDailyVolume10Day { get; set; }
        public DoubleValueWithRawFmt bid { get; set; }
        public DoubleValueWithRawFmt ask { get; set; }
        public IntValueWithTwoFormats bidSize { get; set; }
        public IntValueWithTwoFormats askSize { get; set; }
        public LongValueWithTwoFormats marketCap { get; set; }
        public DoubleValueWithRawFmt fiftyTwoWeekLow { get; set; }
        public DoubleValueWithRawFmt fiftyTwoWeekHigh { get; set; }
        public DoubleValueWithRawFmt priceToSalesTrailing12Months { get; set; }
        public DoubleValueWithRawFmt fiftyDayAverage { get; set; }
        public DoubleValueWithRawFmt twoHundredDayAverage { get; set; }
        public DoubleValueWithRawFmt trailingAnnualDividendRate { get; set; }
        public DoubleValueWithRawFmt trailingAnnualDividendYield { get; set; }
        public string currency { get; set; }
    }

    public class PreMarketChange
    {
    }

    public class PreMarketPrice
    {
    }

    public class PostMarketChange
    {
    }

    public class PostMarketPrice
    {
    }

    public class Price
    {
        public int maxAge { get; set; }
        public PreMarketChange preMarketChange { get; set; }
        public PreMarketPrice preMarketPrice { get; set; }
        public PostMarketChange postMarketChange { get; set; }
        public PostMarketPrice postMarketPrice { get; set; }
        public DoubleValueWithRawFmt regularMarketChangePercent { get; set; }
        public DoubleValueWithRawFmt regularMarketChange { get; set; }
        public int regularMarketTime { get; set; }
        public IntValueWithTwoFormats priceHint { get; set; }
        public DoubleValueWithRawFmt regularMarketPrice { get; set; }
        public DoubleValueWithRawFmt regularMarketDayHigh { get; set; }
        public DoubleValueWithRawFmt regularMarketDayLow { get; set; }
        public LongValueWithTwoFormats regularMarketVolume { get; set; }
        public LongValueWithTwoFormats averageDailyVolume10Day { get; set; }
        public DoubleValueWithRawFmt regularMarketPreviousClose { get; set; }
        public string regularMarketSource { get; set; }
        public DoubleValueWithRawFmt regularMarketOpen { get; set; }

        public string exchange { get; set; }
        public string exchangeName { get; set; }
        public int exchangeDataDelayedBy { get; set; }
        public string marketState { get; set; }
        public string quoteType { get; set; }
        public string symbol { get; set; }
        public string underlyingSymbol { get; set; }
        public string shortName { get; set; }
        public string longName { get; set; }
        public string currency { get; set; }
        public string quoteSourceName { get; set; }
        public string currencySymbol { get; set; }
        public LongValueWithTwoFormats marketCap { get; set; }
    }

    public class DefaultKeyStatistics
    {
        public DoubleValueWithRawFmt trailingEps { get; set; }
        public DoubleValueWithRawFmt forwardEps { get; set; }
        public LongValueWithRawFmt lastDividendDate { get; set; }
        public LongValueWithRawFmt lastSplitDate { get; set; }
        public DoubleValueWithRawFmt forwardPE { get; set; }
        public DoubleValueWithRawFmt lastDividendValue { get; set; }
    }



    public class RapidApiStockDataYFAlternativeDetailsResult
    {
        public RapidApiStockDataYFAlternativeSummaryDetail summaryDetail { get; set; }
        public Price price { get; set; }
        public DefaultKeyStatistics defaultKeyStatistics { get; set; }
    }

    public class RapidApiStockDataYFAlternativeQuoteSummary
    {
        public List<RapidApiStockDataYFAlternativeDetailsResult> result { get; set; }
        public string error { get; set; }
    }








}
