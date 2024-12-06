namespace Common.Util
{
    public static class DivMath
    {
        //Calculates resulting dividends after 'years' years assuming old portfolio continues at growthNow and all new and reinvested divs are at new yield/growth
        public static double CalculateEndDividends(double divsNow, double growthNow, double newYearlyInvestments, double newInvestmentYield, double newGrowth, int years)
        {
            int year = 0;
            double oldDivs = divsNow;
            double newPile = 0.0;
            while (year <= years)
            {
                if (year == 0)
                {
                    oldDivs = divsNow;
                    newPile = 0;
                    year++;
                    continue;
                }

                //old divs growth from increases     dividends on re-invested previous year after-tax dividends at new yield
                oldDivs = oldDivs * (1.0 + growthNow / 100.0) + (1 - 0.255) * newInvestmentYield / 100.0 * oldDivs;
                newPile = newPile * (1.0 + newGrowth / 100.0) + newInvestmentYield / 100.0 * newYearlyInvestments + (1 - 0.255) * newInvestmentYield / 100.0 * newPile;

                //       growth of previous year new dividends     dividends received from new investments and new yield     dividends on re-invested previous year after-tax dividends 
                year++;
            }

            return oldDivs + newPile;
        }


        public static double CalculateEndDividends(double startingPrincipal, double yield, double growth, int years)
        {
            int year = 1;
            double yearlyDivs = 0.0;
            double afterTaxDivs = 0.0;
            while (year <= years)
            {
                if (year == 1)
                {
                    yearlyDivs = yield / 100.0 * startingPrincipal;
                    afterTaxDivs = yearlyDivs * (1.0 - 0.255);
                    year++;
                    continue;
                }

                yearlyDivs = (1.0 + growth / 100.0) * yearlyDivs + yield / 100.0 * afterTaxDivs; //growth of previous year divs + dividends from reinvested previous year after-tax divs.
                afterTaxDivs = yearlyDivs * (1.0 - 0.255);
                year++;
            }

            return yearlyDivs;
        }
    }
}
