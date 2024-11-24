using System.Collections.Generic;
using System.Linq;
using Common.Models.Interfaces;

namespace Common.Models
{
    public class SortingRequestDTO : ISortingRequest
    {
        List<ISortingRequestItem> ISortingRequest.Items => this.Items?.Cast<ISortingRequestItem>().ToList();

        public List<SortingRequestItemDTO> Items { get; set; }
    }

    public class SortingRequestItemDTO : ISortingRequestItem
    {
        public string FieldKey { get; set; }

        public bool IsDescending { get; set; }
    }
}
