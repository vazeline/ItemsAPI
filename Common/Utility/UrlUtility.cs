using System;
using System.Linq;
using Common.ExtensionMethods;

namespace Common.Utility
{
    public static class UrlUtility
    {
        public static string Combine(string url, params string[] segments)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (segments == null || segments.Length == 0)
            {
                return url;
            }

            segments = segments.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            return segments.Aggregate(url, (current, segment) => $"{current.TrimEnd('/')}/{segment.TrimStart('/')}");
        }

        public static string DtoToQueryString(object dto, bool excludeDefaultValueProperties = false)
        {
            return dto
                .GetType()
                .GetProperties()
                .Where(x =>
                {
                    var value = x.GetValue(dto);

                    if (value == null)
                    {
                        return false;
                    }

                    if (!excludeDefaultValueProperties)
                    {
                        return true;
                    }

                    if (value.Equals(TypeUtility.GetDefaultValue(x.PropertyType)))
                    {
                        return false;
                    }

                    return true;
                })
                .Select(x =>
                {
                    var value = x.GetValue(dto);

                    if (x.PropertyType == typeof(DateTime))
                    {
                        value = ((DateTime)value).ToISOString();
                    }
                    else if (Nullable.GetUnderlyingType(x.PropertyType) == typeof(DateTime) && value != null)
                    {
                        value = ((DateTime?)value).Value.ToISOString();
                    }

                    return $"{System.Web.HttpUtility.UrlEncode(x.Name)}={(value == null ? string.Empty : System.Web.HttpUtility.UrlEncode(value.ToString()))}";
                })
                .StringJoin("&");
        }
    }
}
