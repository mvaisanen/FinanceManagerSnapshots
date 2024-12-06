using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFrontendNew.Client.ClientModels
{
    public class ScreenerSearchParams
    {
        public  double CumulativeYoCMin { get; set; }
        public  int DgYearsMin { get; set; }
        public double YieldMin { get; set; }
    }
}
