using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Models.Interfaces;

namespace Common.Models
{
    public class PagingSortingRequestDTO : IPagingSortingRequest
    {
        IPagingRequest IPagingSortingRequest.Paging => this.Paging;

        ISortingRequest IPagingSortingRequest.Sorting => this.Sorting;

        public PagingRequestDTO Paging { get; set; }

        public SortingRequestDTO Sorting { get; set; }
    }
}
