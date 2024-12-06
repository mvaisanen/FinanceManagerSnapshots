//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
//using Newtonsoft.Json;

namespace FinanceManager.Server.Tests.Util
{
    public static class ExtensionMethods
    {
        public static T DeepCopy<T>(this T self)
        {
            //var serialized = JsonConvert.SerializeObject(self);
            var serialized = JsonSerializer.Serialize(self);

            //return JsonConvert.DeserializeObject<T>(serialized);
            return JsonSerializer.Deserialize<T>(serialized);
        }
    }
}
