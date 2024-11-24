using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models.Interfaces;
using Items.Data.EFCore.Abstraction.Interfaces;
using Item = Items.Domain.DomainEntities.Item;

namespace Items.Domain.DomainRepositories.Actions.Interfaces
{
    public interface IItemRepository : IBaseRepository<Item>
    {
        Task<List<Item>> GlobalListAsync( IPagingSortingRequest pagingSorting, int? code, string value );
    }
}