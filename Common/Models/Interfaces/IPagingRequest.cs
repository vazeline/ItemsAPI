using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace Common.Models.Interfaces
{
    public interface IPagingRequest
    {
        public int PageSize { get; }

        public int PageIndex { get; }

        public static IPagingRequest FromQueryString(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
            {
                return null;
            }

            var parsed = QueryHelpers.ParseQuery(queryString).ToDictionary(x => x.Key.ToLower(), x => x.Value.ElementAtOrDefault(0));

            var request = new PagingRequestDTO();

            if (parsed.TryGetValue(nameof(PageSize).ToLower(), out var pageSizeString)
                && int.TryParse(pageSizeString, out var pageSize))
            {
                request.PageSize = pageSize;
            }

            if (parsed.TryGetValue(nameof(PageIndex).ToLower(), out var pageIndexString)
                && int.TryParse(pageIndexString, out var pageIndex))
            {
                request.PageIndex = pageIndex;
            }

            // nothing could be parsed, or default values have been passed
            if (request.PageSize == default && request.PageIndex == default)
            {
                return null;
            }

            return request;
        }
    }
}