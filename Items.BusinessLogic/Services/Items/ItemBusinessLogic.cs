using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Common.Models;
using Items.BusinessLogic.Services.Actions.Interfaces;
using Items.Domain.DomainEntities;
using Items.Domain.DomainRepositories.Actions.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;
using Items.GenericServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Items.BusinessLogic.Services.Actions
{
    public class ItemBusinessLogic : GenericBusinessLogic<Item, IItemsUnitOfWork, IItemRepository>, IItemBusinessLogic
    {
        private readonly IItemsUnitOfWork unitOfWork;

        public ItemBusinessLogic(
            IItemsUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ItemBusinessLogic> logger,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, mapper, logger, httpContextAccessor)
        {
            this.unitOfWork = unitOfWork;
        }

        public override IItemRepository Repository => this.unitOfWork.ItemRepository;

        public async Task<OperationResult> BulkInsertValuesAsync(List<(int Code, string Value)> items)
        {
            var result = new OperationResult();

            var transaction = await this.unitOfWork.BeginTransactionAsync();
            try
            {
                // first truncate table
                var truncateResult = await this.TruncateAsync();
                if (!truncateResult.IsSuccessful)
                {
                    result.AddErrors(truncateResult);
                    await transaction.TryRollbackAsync(result);
                    return result;
                }

                var bulkInsertResult = await this.CreateEntityListAndSaveAsync(
                    unitofwork => Task.FromResult(Item.CreateItemsListAsync(items: items)));

                if (!bulkInsertResult.IsSuccessful)
                {
                    result.AddErrors(bulkInsertResult);
                    await transaction.TryRollbackAsync(result);
                    return result;
                }

                await transaction.CommitAsync(result);
            }
            catch
            {
                await transaction.TryRollbackAsync(result);
                throw;
            }

            return result;
        }

    }
}