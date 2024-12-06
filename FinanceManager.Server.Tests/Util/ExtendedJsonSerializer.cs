using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinanceManager.Server.IntegrationTests.Util
{
    public static class ExtendedJsonSerializer
    {
        //private static JsonSerializerOptions defaultSerializerSettings = new JsonSerializerOptions();

        // set this up how you need to!
        private static JsonSerializerOptions camelCaseSerializerSettings = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, camelCaseSerializerSettings);
        }

        public static T Deserialize<T>(string json, JsonSerializerOptions settings)
        {
            return JsonSerializer.Deserialize<T>(json, settings);
        }
    }
}
