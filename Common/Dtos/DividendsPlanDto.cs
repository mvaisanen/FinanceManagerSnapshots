using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DividendsPlanDto
    {
        public int Id { get; set; }
        public int StartYear { get; set; }
        public int Years { get; set; }
        public double StartDividends { get; set; }
        public double Yield { get; set; }
        public double CurrentDivGrowth { get; set; }
        public double NewDivGrowth { get; set; }
        public double YearlyInvestment { get; set; }
        public Dictionary<string, double> AlternativeYieldsAndGrowthsBasedOnPlan { get; set; }
    }
}
