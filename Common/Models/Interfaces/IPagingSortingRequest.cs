using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.Interfaces
{
    public interface IPagingSortingRequest
    {
        IPagingRequest Paging { get; }

        ISortingRequest Sorting { get; }

        public static IPagingSortingRequest FromQueryString(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
            {
                return null;
            }

            var paging = (PagingRequestDTO)IPagingRequest.FromQueryString(queryString);
            var sorting = (SortingRequestDTO)ISortingRequest.FromQueryString(queryString);

            if (paging == null && sorting == null)
            {
                return null;
            }

            return new PagingSortingRequestDTO
            {
                Paging = (PagingRequestDTO)IPagingRequest.FromQueryString(queryString),
                Sorting = (SortingRequestDTO)ISortingRequest.FromQueryString(queryString)
            };
        }
    }
}
