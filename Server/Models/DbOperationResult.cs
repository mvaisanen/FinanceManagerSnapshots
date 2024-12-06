using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Models
{
    public class DbOperationResult<T>
    {
        public bool Succeeded { get; internal set; }
        public T Item { get; internal set; }
        public string ErrorMsg { get; internal set; }

        public DbOperationResult(T modifiedItem)
        {
            Succeeded = true;
            Item = modifiedItem;
            ErrorMsg = null;
        }

        public DbOperationResult(bool succeeded, T modifiedItem, string errorMsg = null)
        {
            Succeeded = succeeded;
            Item = modifiedItem;
            ErrorMsg = errorMsg;
        }

        public DbOperationResult(bool succeeded, T modifiedItem)
        {
            Succeeded = succeeded;
            Item = modifiedItem;
            ErrorMsg = null;
        }

        public DbOperationResult(string errorMsg)
        {
            Succeeded = false;
            Item = default(T);
            ErrorMsg = errorMsg;
        }
    }
}
