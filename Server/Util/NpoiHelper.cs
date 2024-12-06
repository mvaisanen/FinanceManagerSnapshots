using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Util
{
    public static class NpoiHelper
    {
        public static double GetDoubleValue(ICell cell, IFormulaEvaluator evaluator=null)
        {
            if (cell.CellType == CellType.Numeric)
                return cell.NumericCellValue;
            else if (cell.CellType == CellType.Formula && evaluator != null)
            {
                evaluator.EvaluateFormulaCell(cell);
                try
                {
                    return cell.NumericCellValue;
                }
                catch (InvalidOperationException ioe)
                {
                    if (cell.StringCellValue == "n/a")
                        return 0.0;
                    else
                        throw new ArgumentException("Invalid datafield in cell");
                }
            }
            else if (cell.CellType == CellType.Blank)
                return 0.0;
            else
            {
                var val = cell.StringCellValue;
                if (val == "n/a")
                    return 0.0;
                else
                    throw new ArgumentException("Invalid datafield in cell");
            }
        }
    }
}
