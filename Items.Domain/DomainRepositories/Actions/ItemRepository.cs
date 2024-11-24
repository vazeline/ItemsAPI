using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Utility;
using Items.Domain.DomainRepositories.Actions.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;
using Common.Models.Interfaces;
using Items.Data.EFCore.Abstraction;
using Item = Items.Domain.DomainEntities.Item;
using Common.ExtensionMethods;

namespace Items.Domain.DomainRepositories.Actions
{
    public partial class ItemRepository : BaseRepository<Item, IItemsUnitOfWork>, IItemRepository
    {
        public const string SortFieldKeyNumber = "number";
        public const string SortFieldKeyCode = "code";
        public const string SortFieldKeyValue = "value";

        public ItemRepository(IItemsUnitOfWork unitOfWork)
            : base(unitOfWork)
        {
        }

        protected override Dictionary<string, Expression<Func<Item, object>>> SortFieldKeyMap => new Dictionary<string, Expression<Func<Item, object>>>
        {
            { SortFieldKeyNumber, action => action.Id },
            { SortFieldKeyCode, action => action.Code },
            { SortFieldKeyValue, action => action.Value },
        };

        // probably add more parameters for todo/completed, assigned user, action type etc
        public async Task<List<Item>> GlobalListAsync(
            IPagingSortingRequest pagingSorting,
            int? code,
            string value)
        {
            (pagingSorting?.Paging?.PageSize ?? default).ThrowIfDefault("PageSize parameter is required"); // enforce paging - do not ever want to fetch every single action in the system

            var predicate = await this.GetPredicateForItemsListAsync(  code, value);

            // the projection needs fleshing out here to return sufficient fields for requirement
            return await this.ListByQueryAsync(
                predicate: predicate,
                orderBy: this.SortingRequestToOrderBy(pagingSorting?.Sorting) ?? (x => x.OrderBy(y => y.Code)),
                paging: pagingSorting.Paging,
                projection: x => new Item
                {
                    Value = x.Value,
                    Code = x.Code
                });
        }

        private async Task<Expression<Func<Item, bool>>> GetPredicateForItemsListAsync( int? code, string value )
        {
            var predicateParts = new List<Expression<Func<Item, bool>>>()
            {
                value != null ? (item => item.Value == value ) : null,
                code != null ? (item => code == item.Code ) : null,
            };

            return ExpressionUtility.CombineWithAnd(predicateParts.ToArray());
        }
    }
}