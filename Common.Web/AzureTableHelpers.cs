using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Web
{
    public class AzureTableHelpers
    {
        public static void SetLongString(TableEntity entity, string str, string prefix = "Data")
        {
            var max = 32 * 1024;
            for (int i = 0; i < (int)Math.Ceiling((decimal)str.Length / max); i++)
            {
                var index = i * max;
                entity.Add($"{prefix}{i}", str.Substring(index, Math.Min(max, str.Length - index)));
            }
        }

        public static string GetLongString(TableEntity entity, string prefix = "Data")
        {
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (true)
            {
                if (entity.TryGetValue($"{prefix}{i++}", out var obj))
                    sb.Append(obj.ToString());
                else
                    break;
            }
            return sb.ToString();
        }
    }
}
