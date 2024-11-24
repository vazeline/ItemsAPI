using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Common.Utility.Classes
{
    public static class JsonUtility
    {
        public static bool IsStringValidJSON(string str, out JsonDocument parsed)
        {
            parsed = null;

            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            str = str.Trim();

            if ((str.StartsWith("{") && str.EndsWith("}"))
                || (str.StartsWith("[") && str.EndsWith("]")))
            {
                try
                {
                    parsed = JsonDocument.Parse(str);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
