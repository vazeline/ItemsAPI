using System;
using Common.Models.Interfaces;

namespace Common.Models
{
    public class PagingRequestDTO : IPagingRequest
    {
        public int PageSize { get; set; }

        public int PageIndex { get; set; }
    }
}
