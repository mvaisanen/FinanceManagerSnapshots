using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financemanager.Server.Database.Domain
{
    public class DividendsPlan
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int StartYear { get; set; }
        public int Years { get; set; }
        public double StartDividends { get; set; }
        public double Yield { get; set; }
        public double CurrentDivGrowth { get; set; }
        public double NewDivGrowth { get; set; }
        public double YearlyInvestment { get; set; }
    }
}
