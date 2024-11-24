using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using Items.Domain.DomainEntities;
using Items.Domain.DomainRepositories.Actions.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;
using Items.GenericServices.Interfaces;

namespace Items.BusinessLogic.Services.Actions.Interfaces
{
    public interface IItemBusinessLogic : IGenericBusinessLogic<Item, IItemsUnitOfWork, IItemRepository>
    {
        Task<OperationResult> BulkInsertValuesAsync(List<(int Code, string Value)> list );
    }
}