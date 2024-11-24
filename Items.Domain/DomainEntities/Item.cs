using System.Collections.Generic;
using System.Linq;
using Common.Models;
using Items.Data.EFCore.ExtensionMethods;
using Common.ExtensionMethods;

namespace Items.Domain.DomainEntities
{
    public class Item : ItemsDomainEntityBase
    {
        internal Item()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Item"/> class />.
        /// </summary>
        private Item(
            int code,
            string value)
        {
            this.Code = code;
            this.Value = value;
        }

        public int Code { get; internal set; }

        public string Value { get; internal set; }


        public static OperationResult<List<Item>> CreateItemsListAsync(List<(int Code, string Value)> items)
        {
            var result = new OperationResult<List<Item>>();

            foreach (var (code, value) in items)
            {
                result
                    .Validate(value, ValidationExtensions.StringIsNotNullOrWhiteSpace);
            }

            if (!result.IsSuccessful)
            {
                return result;
            }

            result.Data = new List<Item>();

            foreach (var item in items.OrderBy(i => i.Code))
            {
                var dataitem = new Item(
                    item.Code,
                    item.Value);

                result.Data.Add(dataitem);
            }

            return result;
        }

    }
}