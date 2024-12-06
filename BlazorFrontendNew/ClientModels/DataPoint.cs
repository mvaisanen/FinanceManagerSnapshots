using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFrontendNew.Client.ClientModels
{
    public class DataPoint
    {
        public  double Value { get; private set; }
        public  string Name { get; private set; }

        public DataPoint(double value, string name)
        {
            Value = value;
            Name = name;
        }
    }

    public class DoubleDataPoint
    {
        public double Value1 { get; private set; }
        public double Value2 { get; private set; }
        public string Name { get; private set; }

        public DoubleDataPoint(double value1, double value2, string name)
        {
            Value1 = value1;
            Value2 = value2;
            Name = name;
        }
    }
}
