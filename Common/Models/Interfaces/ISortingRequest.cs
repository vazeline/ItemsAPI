using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Common.ExtensionMethods;
using Microsoft.AspNetCore.WebUtilities;

namespace Common.Models.Interfaces
{
    public interface ISortingRequest
    {
        public List<ISortingRequestItem> Items { get; }

        public string ToQueryString()
        {
            if (this.Items?.Any() != true)
            {
                return null;
            }

            return $"orderby={this.Items.Select(x => HttpUtility.UrlEncode(x.FieldKey.ToLower()) + (x.IsDescending ? " desc" : string.Empty)).StringJoin(",")}";
        }

        public static ISortingRequest FromQueryString(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
            {
                return null;
            }

            var parsed = QueryHelpers.ParseQuery(queryString).ToDictionary(x => x.Key.ToLower(), x => x.Value.ElementAtOrDefault(0));

            if (parsed.TryGetValue("orderby", out var itemsString) && !string.IsNullOrWhiteSpace(itemsString))
            {
                var request = new SortingRequestDTO
                {
                    Items = new List<SortingRequestItemDTO>()
                };

                foreach (var itemString in itemsString.Replace(';', ',').Split(','))
                {
                    var itemStringSplit = itemString.Split(' ');

                    if (itemStringSplit.Length == 1 && !string.IsNullOrWhiteSpace(itemStringSplit[0]))
                    {
                        request.Items.Add(new SortingRequestItemDTO
                        {
                            FieldKey = itemStringSplit[0]
                        });
                    }
                    else if (itemStringSplit.Length == 2 && !string.IsNullOrWhiteSpace(itemStringSplit[0]))
                    {
                        request.Items.Add(new SortingRequestItemDTO
                        {
                            FieldKey = itemStringSplit[0],
                            IsDescending = !string.IsNullOrWhiteSpace(itemStringSplit[1]) && itemStringSplit[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }

                return request;
            }

            return null;
        }
    }

    public interface ISortingRequestItem
    {
        public string FieldKey { get; }

        public bool IsDescending { get; }
    }
}
