using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Util
{
    public class StringRoutines
    {
        //Improved csv-splitter that can handle separators inside quotation marks
        public static string[] SplitLine(string line, char separator)
        {
            List<string> fields = new List<string>();
            int idx = 0;
            string field = "";
            bool quotationActive = false;
            while (idx < line.Length)
            {
                char mark = line[idx];

                if (mark == '"' && quotationActive == false)
                    quotationActive = true;
                else if (mark == '"' && quotationActive == true)
                    quotationActive = false;

                if (mark == separator && quotationActive == false)
                {
                    fields.Add(field);
                    field = "";
                    quotationActive = false; //turhaa...
                }
                else if (mark != '"') //ei lisätä heittomerkkejä, sql ei tykkää
                    field = field + mark;

                idx++;

                //TODO: We might be missing the last field when exiting at end of line and there's no ;-mark         
            }

            string[] retVals = new string[fields.Count];
            for (int i = 0; i < fields.Count; i++)
            {
                retVals[i] = fields[i];
            }

            return retVals;
        }
    }
}
